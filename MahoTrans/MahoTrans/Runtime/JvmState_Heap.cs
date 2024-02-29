// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using java.lang;
using JetBrains.Annotations;
using MahoTrans.Abstractions;
using MahoTrans.Runtime.Config;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Exceptions;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Array = System.Array;
using Object = java.lang.Object;
using String = java.lang.String;
using WeakReference = java.lang.@ref.WeakReference;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    public StaticMemory StaticMemory = new();
    private Object?[] _heap = new Object?[1024 * 16];
    public long[] StaticFields = Array.Empty<long>();
    public List<Field> StaticFieldsOwners = new();
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

    #region Object/string allocation

    /// <summary>
    ///     Creates new CLR object for given JVM class. This used by interpreter. This can be used by native code if caller
    ///     doesn't know what it is doing.
    /// </summary>
    /// <param name="class">Class object.</param>
    /// <returns>Reference to newly created object.</returns>
    /// <remarks>
    ///     As per benchmark, this appears to be x2 faster than generic <see cref="Allocate{T}" />:
    ///     https://t.me/sym_ansel_dev/39 . This is because generic call does dictionary lookup to obtain java class object.
    /// </remarks>
    public Reference AllocateObject(JavaClass @class)
    {
        Object o = (Activator.CreateInstance(@class.ClrType!) as Object)!;
        o.JavaClass = @class;
        return PutToHeap(o);
    }

    /// <summary>
    ///     Creates new CLR object of CLR class that represents given JVM type.
    /// </summary>
    /// <typeparam name="T">Class to instantiate.</typeparam>
    /// <returns>Newly created object.</returns>
    /// <remarks>
    ///     This calls default constructor, assigns java class object and adds the object to heap. This is easier to use in
    ///     native code, but <see cref="AllocateObject" /> is faster.
    /// </remarks>
    public T Allocate<T>() where T : Object, new()
    {
        var o = new T();
        o.JavaClass = Classes[typeof(T).ToJavaName()];
        PutToHeap(o);
        return o;
    }

    /// <summary>
    ///     Wraps CLR string into JVM object.
    /// </summary>
    /// <param name="str">String to wrap.</param>
    /// <returns>Created string object.</returns>
    public Reference AllocateString(string str)
    {
        return PutToHeap(new String
        {
            Value = str,
            JavaClass = Classes["java/lang/String"]
        });
    }

    /// <summary>
    ///     Wraps CLR string into JVM object. Attempts to use cached "internalized" string.
    /// </summary>
    /// <param name="str">String to wrap.</param>
    /// <returns>Reference to internalized string wrapper.</returns>
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
    ///     Creates JVM array wrapper over CLR array. This is for native code.
    /// </summary>
    /// <param name="data">CLR array to put it. It won't be copied.</param>
    /// <param name="className">Array class. If you store strings, this must be "[Ljava/lang/String;".</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference WrapReferenceArray(Reference[] data, string className)
    {
        if (data == null!)
            throw new JavaRuntimeError("Attempt to wrap null array");

        JavaClass cls = GetClass(className);

        return PutToHeap(Array<Reference>.Create(data, cls));
    }

    /// <summary>
    ///     Creates JVM array wrapper over CLR array of primitives (ints, chars, etc.). This is for native code.
    /// </summary>
    /// <param name="data">CLR array to put it. It won't be copied.</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference WrapPrimitiveArray<T>(T[] data) where T : struct
    {
        if (data == null!)
            throw new JavaRuntimeError("Attempt to wrap null array");

        // this does all the checks (i.e. sbyte/ubyte)
        var arrayClass = PrimitiveToArrayType<T>();

        return PutToHeap(Array<T>.Create(data, arrayClass));
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
            ArrayType.T_BOOLEAN => AllocateArrayInternal<bool>(length),
            ArrayType.T_CHAR => AllocateArrayInternal<char>(length),
            ArrayType.T_FLOAT => AllocateArrayInternal<float>(length),
            ArrayType.T_DOUBLE => AllocateArrayInternal<double>(length),
            ArrayType.T_BYTE => AllocateArrayInternal<sbyte>(length),
            ArrayType.T_SHORT => AllocateArrayInternal<short>(length),
            ArrayType.T_INT => AllocateArrayInternal<int>(length),
            ArrayType.T_LONG => AllocateArrayInternal<long>(length),
            _ => Reference.Null
        };
    }

    /// <summary>
    ///     Creates an array. This is used by interpreter.
    /// </summary>
    /// <param name="length">Length of the array. Will be validated.</param>
    /// <param name="cls">Array class. If you store strings, this must be "[Ljava/lang/String;".</param>
    /// <returns>Reference to newly created array.</returns>
    public Reference AllocateReferenceArray(int length, JavaClass cls)
    {
        if (length < 0)
            Throw<NegativeArraySizeException>();
        return PutToHeap(Array<Reference>.CreateEmpty(length, cls));
    }

    #endregion

    #region Array allocation (utils)

    private Reference AllocateArrayInternal<T>(int length) where T : struct
    {
        if (typeof(T) == typeof(Reference))
            throw new JavaRuntimeError("Reference array must have assigned class!");
        return PutToHeap(Array<T>.CreateEmpty(length, PrimitiveToArrayType<T>()));
    }

    /// <summary>
    ///     Gets JVM class for "[primitive" object. Must not be used with references.
    /// </summary>
    /// <typeparam name="T">CLR type to get type for.</typeparam>
    /// <returns>JVM class.</returns>
    /// <exception cref="JavaRuntimeError">Something wrong is passed.</exception>
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
        else if (typeof(T) == typeof(byte))
            throw new JavaRuntimeError("Attempt to allocate array of unsigned bytes");
        else if (typeof(T) == typeof(Reference))
            throw new JavaRuntimeError("Attempt to allocate reference array as primitive array");
        else
            throw new JavaRuntimeError($"Attempt to allocate an array of non-supported primitive type {typeof(T)}");
        return GetClass(name);
    }

    #endregion

    #region Resolution

    public Object ResolveObject(Reference r)
    {
#if DEBUG
        return Resolve<Object>(r);
#else
        if (r.IsNull)
            Throw<NullPointerException>();
        return _heap[r.Index]!;
#endif
    }


    public T Resolve<T>(Reference r) where T : Object
    {
        if (r.IsNull)
            Throw<NullPointerException>();
#if DEBUG
        if (r.Index >= _heap.Length)
            throw new JavaRuntimeError($"Reference {r.Index} is out of bounds ({_heap.Length})");
        var obj = _heap[r.Index];
        if (obj == null)
            throw new JavaRuntimeError($"Reference {r.Index} pointers to null object, {typeof(T)} expected.");

        if (obj is T t)
            return t;

        throw new JavaRuntimeError($"Reference {r.Index} pointers to {obj.GetType()} object, {typeof(T)} expected.");
#else
        return Unsafe.As<T>(_heap[r.Index]!);
#endif
    }

    public T? ResolveOrNull<T>(Reference r) where T : Object
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
        var obj = Unsafe.As<String>(_heap[r.Index]!);
        return obj.Value;
    }

    /// <summary>
    ///     Resolves a string and gets its value as <see cref="string" />. If reference is null, broken, or object is not a
    ///     string, this silently returns null.
    /// </summary>
    /// <param name="r">Reference to resolve.</param>
    /// <returns>String value.</returns>
    public string? ResolveStringOrNull(Reference r)
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
        var obj = Unsafe.As<Array<T>>(_heap[r.Index]!);
        return obj.Value;
    }

    public T[]? ResolveArrayOrNull<T>(Reference r) where T : struct
    {
        if (r.IsNull)
            return null;
        var obj = _heap[r.Index] as Array<T>;
        return obj?.Value;
    }

    /// <summary>
    ///     Helper for interpreter and bridges.
    /// </summary>
    public void SetArrayElement<T>(Reference r, int index, T value) where T : struct
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        var obj = Unsafe.As<Array<T>>(_heap[r.Index]!);
        obj[index] = value;
    }

    #endregion

    #region Exceptions

    /// <summary>
    ///     Throws a java exception. It's expected that <see cref="JavaRunner.ProcessThrow" /> will catch and process it.
    /// </summary>
    /// <typeparam name="T">Type of exception.</typeparam>
    /// <exception cref="JavaThrowable">Always thrown CLR exception. Contains java exception to be processed by handler.</exception>
    [DoesNotReturn]
    public void Throw<T>() where T : Throwable, new()
    {
        var ex = Allocate<T>();
        ex.Init();
        ex.Source = ThrowSource.Native;
        Toolkit.Logger?.LogExceptionThrow(ex.This);
        throw new JavaThrowable(ex);
    }

    [DoesNotReturn]
    public void Throw<T>(string message) where T : Throwable, new()
    {
        var ex = Allocate<T>();
        ex.Init(AllocateString(message));
        ex.Source = ThrowSource.Native;
        Toolkit.Logger?.LogExceptionThrow(ex.This);
        throw new JavaThrowable(ex);
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
                        if (field.NativeField != null)
                        {
                            if (field.NativeField.FieldType == typeof(Reference))
                            {
                                enumQueue.Enqueue((Reference)field.NativeField.GetValue(o)!);
                            }
                        }
                    }
                }
            }

            int deletedCount = 0;

            // creating weak refs map
            // key - stored reference (where this Ref obj points to), values - the Ref objects.
            // so, if there are 2 weakrefs pointing at object 25, this will be [25]={w1,w2}
            // TODO pool/cache lists/dicts to reduce allocations?
            Dictionary<int, List<WeakReference>> weakRefsMap = new();
            foreach (var o in _heap)
            {
                if (o is WeakReference w)
                {
                    if (weakRefsMap.TryGetValue(w.StoredReference, out var l))
                    {
                        l.Add(w);
                    }
                    else
                    {
                        weakRefsMap[w.StoredReference] = new List<WeakReference> { w };
                    }
                }
            }

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
                    if (weakRefsMap.TryGetValue(i, out var l))
                    {
                        foreach (var w in l)
                        {
                            w.StoredReference = 0;
                        }
                    }

                    _heap[i] = null;
                }
            }

            GcCount++;
            Toolkit.Logger?.LogEvent(EventCategory.Gc,
                $"Deleted {deletedCount} objects");
        }
    }

    /// <summary>
    ///     Helper for GC. Collects all references to objects which must stay alive.
    /// </summary>
    /// <returns>List of references. This may contain null and invalid references (i.e. random numbers which point to nowhere).</returns>
    public unsafe List<Reference> CollectObjectGraphRoots()
    {
        List<Reference> roots = StaticMemory.GetAll();

        roots.Add(MidletObject);

        // building roots list
        {
            // classes
            foreach (var cls in Classes.Values)
            {
                if (!cls.ModelObject.IsNull)
                {
                    roots.Add(cls.ModelObject);
                }
            }

            foreach (var field in StaticFields)
            {
                if (field > 0 && field <= _heap.Length)
                    roots.Add(field);
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

    /// <summary>
    ///     Sets object in heap to null. No checks are done. No events are invoked. This is for internal usage.
    /// </summary>
    /// <param name="r">Reference to clear.</param>
    /// <remarks>This is for internal usage only.</remarks>
    [Obsolete("Objects must not be deleted from heap directly. Never call this outside from tests/benchmarks.")]
    public void ForceDeleteObject(Reference r)
    {
        _heap[r] = null;
    }

    #endregion
}