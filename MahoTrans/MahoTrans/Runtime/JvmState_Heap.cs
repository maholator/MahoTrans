// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using java.lang;
using JetBrains.Annotations;
using MahoTrans.Abstractions;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Array = System.Array;
using Object = java.lang.Object;
using String = java.lang.String;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    private Object?[] _heap = new Object?[1024 * 16];
    private int _nextObjectId = 1;
    [PublicAPI] public int ObjectsOnFly;
    private Dictionary<string, int> _internalizedStrings = new();

    private int _bytesAllocated;

    [PublicAPI] public int BytesAllocated => _bytesAllocated;

    //TODO
    public int TotalMemory { get; } = 1024 * 1024 * 4;
    public int FreeMemory => TotalMemory - BytesAllocated;

    [PublicAPI] public int GcCount;

    private bool _gcPending;

    #region Object allocation

    public Reference AllocateObject(JavaClass @class)
    {
        Object o = (Activator.CreateInstance(@class.ClrType!) as Object)!;
        o.JavaClass = @class;
        return PutToHeap(o);
    }

    public T AllocateObject<T>() where T : Object
    {
        var o = Activator.CreateInstance<T>();
        o.JavaClass = Classes[typeof(T).ToJavaName()];
        PutToHeap(o);
        return o;
    }

    public Reference AllocateString(string str)
    {
        return PutToHeap(new String
        {
            Value = str,
            JavaClass = Classes["java/lang/String"]
        });
    }

    public Reference InternalizeString(string str)
    {
        if (_internalizedStrings.TryGetValue(str, out var i))
            return new Reference(i);
        var r = AllocateString(str);
        _internalizedStrings[str] = r.Index;
        return r;
    }

    #endregion

    #region Array allocation (for native)

    /// <summary>
    ///     Creates an array. This is for native code.
    /// </summary>
    /// <param name="data">CLR array to put it. It won't be copied.</param>
    /// <param name="cls">Array class. If you store strings, this must be "[java/lang/String".</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocateArray<T>(T[] data, string cls) where T : struct =>
        AllocateArray(data, GetClass(cls));

    /// <summary>
    ///     Creates an array. This is for native code.
    /// </summary>
    /// <param name="data">CLR array to put it. It won't be copied.</param>
    /// <param name="cls">Array class. If you store strings, this must be "[java/lang/String".</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocateArray<T>(T[] data, JavaClass cls) where T : struct
    {
        if (data == null!)
            throw new JavaRuntimeError("Attempt to convert null array");

        if (typeof(T) == typeof(byte))
            throw new JavaRuntimeError("Attempt to allocate array of unsigned bytes!");

        return PutToHeap(new Array<T>
        {
            Value = data,
            JavaClass = cls
        });
    }

    /// <summary>
    ///     Creates an array of primitives (ints, chars, etc.). This is for native code.
    /// </summary>
    /// <param name="data">CLR array to put it. It won't be copied.</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocatePrimitiveArray<T>(T[] data) where T : struct
    {
        if (data == null!)
            throw new JavaRuntimeError("Attempt to convert null array");
        if (typeof(T) == typeof(byte))
            throw new JavaRuntimeError("Attempt to allocate array of unsigned bytes!");
        if (typeof(T) == typeof(Reference))
            throw new JavaRuntimeError("Reference array must have assigned class!");
        return PutToHeap(new Array<T>
        {
            Value = data,
            JavaClass = PrimitiveToArrayType<T>(),
        });
    }

    #endregion

    #region Array allocation (for interpreter)

    /// <summary>
    ///     Creates an array. This is used by interpreter.
    /// </summary>
    /// <param name="arrayType">Array type.</param>
    /// <param name="length">Length of the array. Will be validated.</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocateArray(ArrayType arrayType, int length)
    {
        if (length < 0)
            Throw<NegativeArraySizeException>();
        return arrayType switch
        {
            ArrayType.T_BOOLEAN => AllocateArray<bool>(length),
            ArrayType.T_CHAR => AllocateArray<char>(length),
            ArrayType.T_FLOAT => AllocateArray<float>(length),
            ArrayType.T_DOUBLE => AllocateArray<double>(length),
            ArrayType.T_BYTE => AllocateArray<sbyte>(length),
            ArrayType.T_SHORT => AllocateArray<short>(length),
            ArrayType.T_INT => AllocateArray<int>(length),
            ArrayType.T_LONG => AllocateArray<long>(length),
            _ => Reference.Null
        };
    }

    /// <summary>
    ///     Creates an array. This is used by interpreter.
    /// </summary>
    /// <param name="length">Length of the array. Will be validated.</param>
    /// <param name="cls">Array class. If you store strings, this must be "[java/lang/String".</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocateReferenceArray(int length, JavaClass cls)
    {
        if (length < 0)
            Throw<NegativeArraySizeException>();
        return PutToHeap(new Array<Reference>
        {
            Value = new Reference[length],
            JavaClass = cls
        });
    }

    #endregion

    #region Array allocation (utils)

    private Reference AllocateArray<T>(int length) where T : struct
    {
        if (typeof(T) == typeof(Reference))
            throw new JavaRuntimeError("Reference array must have assigned class!");
        return PutToHeap(new Array<T>
        {
            Value = new T[length],
            JavaClass = PrimitiveToArrayType<T>(),
        });
    }

    private JavaClass PrimitiveToArrayType<T>()
    {
        string name;
        if (typeof(T) == typeof(int))
            name = "[I";
        else if (typeof(T) == typeof(long))
            name = "[J";
        else if (typeof(T) == typeof(float))
            name = "[F";
        else if (typeof(T) == typeof(double))
            name = "[D";
        else if (typeof(T) == typeof(short))
            name = "[S";
        else if (typeof(T) == typeof(char))
            name = "[C";
        else if (typeof(T) == typeof(bool))
            name = "[Z";
        else if (typeof(T) == typeof(sbyte))
            name = "[B";
        else
            throw new JavaRuntimeError($"Attempt to allocate an array of non-supported primitive type {typeof(T)}");
        return GetClass(name);
    }

    #endregion

    #region Resolution

    public Object ResolveObject(Reference r)
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        return _heap[r.Index]!;
    }

    public T Resolve<T>(Reference r) where T : Object
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        return (T)_heap[r.Index]!;
    }

    public T? ResolveNullable<T>(Reference r) where T : Object
    {
        if (r.IsNull)
            return null;
        return _heap[r.Index] as T;
    }

    /// <summary>
    ///     Resolves a string and gets its value as <see cref="string" />. Will throw on invalid reference.
    /// </summary>
    /// <param name="r">Reference to resolve.</param>
    /// <returns>String value.</returns>
    public string ResolveString(Reference r)
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        var obj = (String)_heap[r.Index]!;
        return obj.Value;
    }

    /// <summary>
    ///     Resolves a string and gets its value as <see cref="string" />. If reference is null, broken, or object is not a
    ///     string, this silently returns null.
    /// </summary>
    /// <param name="r">Reference to resolve.</param>
    /// <returns>String value.</returns>
    public string? ResolveStringOrDefault(Reference r)
    {
        if (r.IsNull)
            return null;
        var obj = _heap[r.Index] as String;
        return obj?.Value;
    }

    public T[] ResolveArray<T>(Reference r) where T : struct
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        var obj = (Array<T>)_heap[r.Index]!;
        return obj.Value;
    }

    #endregion

    #region Exceptions

    /// <summary>
    ///     Throws a java exception. It's expected that <see cref="JavaRunner.ProcessThrow" /> will catch and process it.
    /// </summary>
    /// <typeparam name="T">Type of exception.</typeparam>
    /// <exception cref="JavaThrowable">Always thrown CLR exception. Contains java exception to be processed by handler.</exception>
    [DoesNotReturn]
    public void Throw<T>() where T : Throwable
    {
        Toolkit.Logger?.LogDebug(DebugMessageCategory.Exceptions, $"{typeof(T).Name} is thrown via native method");
        var ex = AllocateObject<T>();
        ex.Init();
        throw new JavaThrowable(ex.This);
    }

    #endregion

    #region Utils

    public void PushClassConstant(Frame frame, object o)
    {
        switch (o)
        {
            case string s:
                frame.PushReference(InternalizeString(s));
                return;
            case int i:
                frame.PushInt(i);
                return;
            case long l:
                frame.PushLong(l);
                return;
            case float f:
                frame.PushFloat(f);
                return;
            case double d:
                frame.PushDouble(d);
                return;
            default:
                throw new JavaRuntimeError($"{o.GetType()} can't be wrapped. Try using it as is.");
        }
    }

    /// <summary>
    ///     Takes place in the heap. Assigns taken address to passed object. Adds passed object to heap.
    /// </summary>
    /// <param name="obj">Object to add into heap.</param>
    /// <returns>Reference to the object.</returns>
    /// <remarks>This api should not be used. Allocate objects using <see cref="AllocateObject" />.</remarks>
    public Reference PutToHeap(Object obj)
    {
        lock (this)
        {
            if (ObjectsOnFly == _heap.Length - 1)
            {
                switch (OnOverflow)
                {
                    case AllocatorBehaviourOnOverflow.Expand:
                        var newHeap = new Object[_heap.Length * 2];
                        Array.Copy(_heap, newHeap, _heap.Length);
                        _heap = newHeap;
                        break;
                    case AllocatorBehaviourOnOverflow.ThrowOutOfMem:
                        Throw<OutOfMemoryError>();
                        break;
                    case AllocatorBehaviourOnOverflow.Crash:
                        throw new JavaRuntimeError(
                            $"Could not find empty slot for {obj.JavaClass.Name}: {ObjectsOnFly}/{_heap.Length} slots used.");
                }
            }

            if (_nextObjectId % 2000 == 0)
            {
                // run GC every 2k object
                _gcPending = true;
            }

            while (_heap[_nextObjectId] != null)
            {
                // slot taken
                _nextObjectId++;
                if (_nextObjectId == _heap.Length)
                    _nextObjectId = 1;
            }

            var r = new Reference(_nextObjectId);
            obj.HeapAddress = _nextObjectId;
            _heap[_nextObjectId] = obj;

            _bytesAllocated += obj.JavaClass.Size;

            _nextObjectId++;
            if (_nextObjectId == _heap.Length)
                _nextObjectId = 1;

            ObjectsOnFly++;
            Toolkit.HeapDebugger?.ObjectCreated(obj.This);

            return r;
        }
    }

    #endregion

    #region GC

    /// <summary>
    ///     Performs collection in this heap. This must be called only when jvm is stopped!
    /// </summary>
    public void RunGarbageCollector()
    {
        lock (this)
        {
            //TODO optimization

            Stopwatch sw = new();
            sw.Start();
            var roots = CollectObjectGraphRoots();
            Queue<Reference> enumQueue = new Queue<Reference>(roots);

            while (enumQueue.Count > 0)
            {
                var r = enumQueue.Dequeue();

                // invalid references?

                if (r.Index <= 0 || r.Index >= _heap.Length)
                    continue;
                var o = _heap[r.Index];
                if (o == null)
                    continue;

                // this object already marked?

                if (o.Alive)
                    continue;

                // marking as alive
                o.Alive = true;

                // this object is a root now: enumerating subtree
                List<JavaClass> classes = new List<JavaClass>();
                var c = o.JavaClass;

                while (true)
                {
                    classes.Add(c);
                    if (c.IsObject)
                        break;
                    c = c.Super;
                }


                o.AnnounceHiddenReferences(enumQueue);

                foreach (var cls in classes)
                {
                    foreach (var field in cls.Fields.Values)
                    {
                        if (field.NativeField.FieldType == typeof(Reference) && !field.NativeField.IsStatic)
                        {
                            enumQueue.Enqueue((Reference)field.NativeField.GetValue(o)!);
                        }
                    }
                }
            }

            int deletedCount = 0;

            // we marked all alive objects as alive. Time to delete dead ones.

            for (int i = 0; i < _heap.Length; i++)
            {
                var obj = _heap[i];
                if (obj == null)
                    continue;
                if (obj.Alive)
                {
                    obj.Alive = false;
                }
                else
                {
                    if (obj.OnObjectDelete())
                        continue;
                    deletedCount++;
                    ObjectsOnFly--;
                    _bytesAllocated -= obj.JavaClass.Size;
                    Toolkit.HeapDebugger?.ObjectDeleted(obj.This);
                    _heap[i] = null;
                }
            }

            sw.Stop();
            GcCount++;
            Toolkit.Logger?.LogDebug(DebugMessageCategory.Gc,
                $"Deleted {deletedCount} objects in {sw.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    ///     Helper for GC. Collects all references to objects which must stay alive.
    /// </summary>
    /// <returns>List of references. This may contain null and invalid references (i.e. random numbers which point to nowhere).</returns>
    public unsafe List<Reference> CollectObjectGraphRoots()
    {
        List<Reference> roots = new List<Reference>();

        roots.Add(MidletObject);

        // building roots list
        {
            // statics and classes
            foreach (var cls in Classes.Values)
            {
                if (!cls.ModelObject.IsNull)
                {
                    roots.Add(cls.ModelObject);
                }

                foreach (var field in cls.Fields.Values)
                {
                    if ((field.Flags & FieldFlags.Static) != 0)
                    {
                        if (field.NativeField.FieldType == typeof(Reference))
                        {
                            roots.Add((Reference)field.NativeField.GetValue(null)!);
                        }
                    }
                }

                cls.StaticAnnouncer?.Invoke(roots);
            }

            // threads
            foreach (var thread in AliveThreads.Concat(WaitingThreads.Values).Concat(_wakeingUpQueue))
            {
                roots.Add(thread.Model);
                var frames = thread.CallStack.Take(thread.ActiveFrameIndex + 1);

                foreach (var frame in frames)
                {
                    if (frame == null)
                        continue;

                    var top = frame.Method.StackSize;

                    for (int i = 0; i < top; i++)
                    {
                        //TODO we know stack types. They must be used.
                        roots.Add(frame.Stack[i]);
                    }

                    for (int i = 0; i < frame.Method.LocalsCount; i++)
                    {
                        long variable = frame.LocalVariables[i];
                        //TODO push only references
                        if (variable != 0)
                            roots.Add(variable);
                    }
                }
            }

            // internal strings
            foreach (var str in _internalizedStrings.Values)
            {
                roots.Add(new Reference(str));
            }
        }

        return roots;
    }

    #endregion
}