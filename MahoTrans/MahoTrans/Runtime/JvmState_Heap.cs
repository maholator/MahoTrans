using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using java.lang;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    private Dictionary<int, Object> _heap = new();
    private int _nextObjectId = 1;
    private Dictionary<string, int> _internalizedStrings = new();

    #region Allocation

    public Reference AllocateObject(JavaClass @class)
    {
        Object o = (Activator.CreateInstance(@class.ClrType!) as Object)!;
        o.JavaClass = @class;
        return PutToHeap(o);
    }

    public T AllocateObject<T>() where T : Object
    {
        var o = Activator.CreateInstance<T>();
        o.JavaClass = Classes.Values.First(x => x.ClrType == typeof(T));
        PutToHeap(o);
        return o;
    }

    public Reference AllocateString(string str)
    {
        return PutToHeap(new java.lang.String
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

    public Reference AllocateArray<T>(int length) where T : struct
    {
        if (typeof(T) == typeof(Reference))
            throw new JavaRuntimeError("Reference array must have assigned class!");
        return PutToHeap(new Array<T>
        {
            Value = new T[length],
            JavaClass = PrimitiveToArrayType<T>(),
        });
    }

    public Reference AllocateReferenceArray(int length, JavaClass cls)
    {
        return PutToHeap(new Array<Reference>
        {
            Value = new Reference[length],
            JavaClass = cls
        });
    }

    public Reference AllocateArray<T>(T[] data, string cls) where T : struct =>
        AllocateArray<T>(data, GetClass(cls));

    public Reference AllocateArray<T>(T[] data, JavaClass cls) where T : struct
    {
        return PutToHeap(new Array<T>
        {
            Value = data,
            JavaClass = cls
        });
    }

    public Reference AllocateArray(ArrayType arrayType, int len)
    {
        return arrayType switch
        {
            ArrayType.T_BOOLEAN => AllocateArray<bool>(len),
            ArrayType.T_CHAR => AllocateArray<char>(len),
            ArrayType.T_FLOAT => AllocateArray<float>(len),
            ArrayType.T_DOUBLE => AllocateArray<double>(len),
            ArrayType.T_BYTE => AllocateArray<sbyte>(len),
            ArrayType.T_SHORT => AllocateArray<short>(len),
            ArrayType.T_INT => AllocateArray<int>(len),
            ArrayType.T_LONG => AllocateArray<long>(len),
            _ => throw new ArgumentOutOfRangeException(nameof(arrayType), arrayType, null)
        };
    }

    public JavaClass PrimitiveToArrayType<T>()
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
            throw new ArgumentException();
        return GetClass(name);
    }

    #endregion

    #region Resolution

    public Object ResolveObject(Reference r)
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        return _heap[r.Index];
    }

    public T Resolve<T>(Reference r) where T : Object
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        return (T)_heap[r.Index];
    }

    public string ResolveString(Reference r)
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        var obj = (java.lang.String)_heap[r.Index];
        return obj.Value;
    }

    public T[] ResolveArray<T>(Reference r) where T : struct
    {
        if (r.IsNull)
            Throw<NullPointerException>();
        var obj = (Array<T>)_heap[r.Index];
        return obj.Value;
    }

    #endregion

    #region Exceptions

    [DoesNotReturn]
    public void Throw<T>() where T : Throwable
    {
        var ex = AllocateObject<T>();
        throw new JavaThrowable(ex.This);
    }

    #endregion

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
    /// Takes place in the heap. Assigns taken address to passed object. Adds passed object to heap.
    /// </summary>
    /// <param name="obj">Object to add into heap.</param>
    /// <returns>Reference to the object.</returns>
    private Reference PutToHeap(Object obj)
    {
        lock (this)
        {
            if (_nextObjectId % 2000 == 0)
            {
                // run GC every 2k object
                RunGarbageCollector();
            }

            var r = new Reference(_nextObjectId);
            obj.HeapAddress = _nextObjectId;
            _heap[_nextObjectId] = obj;
            _nextObjectId++;
            return r;
        }
    }

    /// <summary>
    /// Performs collection in this heap. This must be called only when jvm is stopped!
    /// </summary>
    public void RunGarbageCollector()
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

            if (r.IsNull)
                continue;
            if (!_heap.TryGetValue(r.Index, out var o))
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

        var newHeap = new Dictionary<int, Object>(_heap.Count);

        foreach (var i in _heap.Keys)
        {
            var obj = _heap[i];
            if (obj.Alive)
            {
                obj.Alive = false;
                newHeap.Add(i, obj);
            }
            else
            {
                deletedCount++;
            }
        }

        _heap = newHeap;

        sw.Stop();
        Console.WriteLine(
            $"GC collected {deletedCount} objects in {sw.ElapsedTicks / 1000} us, {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>
    /// Helper for GC. Collects all references to objects which must stay alive.
    /// </summary>
    /// <returns>List of references. This may contain null and invalid references (i.e. random numbers which point to nowhere).</returns>
    public List<Reference> CollectObjectGraphRoots()
    {
        List<Reference> roots = new List<Reference>();

        // building roots list
        {
            // statics
            foreach (var cls in Classes.Values)
            {
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
            foreach (var thread in AliveThreads.Concat(WaitingThreads.Values))
            {
                roots.Add(thread.Model);
                var frames = thread.CallStack.Take(thread.ActiveFrameIndex + 1);

                foreach (var frame in frames)
                {
                    if (frame == null)
                        continue;

                    var top = frame.Stack.Length;

                    for (int i = 0; i < top; i++)
                    {
                        if ((frame.StackTypes[i] & PrimitiveType.IsReference) != 0)
                        {
                            roots.Add(frame.Stack[i]);
                        }
                    }

                    foreach (var variable in frame.LocalVariables)
                    {
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
}