using System.Diagnostics.CodeAnalysis;
using java.lang;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public class JavaHeap
{
    public readonly JvmState State;
    private Dictionary<int, Object> _heap = new();
    private int _nextObjectId = 1;
    private Dictionary<string, int> _internalizedStrings = new();

    public JavaHeap(JvmState state) => State = state;

    #region Allocation

    public Reference AllocateObject(JavaClass @class)
    {
        var r = TakePlace();
        Object o = (Activator.CreateInstance(@class.ClrType!) as Object)!;
        o.HeapAddress = r.Index;
        o.JavaClass = @class;
        _heap[r.Index] = o;
        return r;
    }

    public T AllocateObject<T>() where T : Object
    {
        var r = TakePlace();
        var o = Activator.CreateInstance<T>();
        o.HeapAddress = r.Index;
        o.JavaClass = State.Classes.Values.First(x => x.ClrType == typeof(T));
        _heap[r.Index] = o;
        return o;
    }

    public Reference AllocateString(string str)
    {
        var r = TakePlace();
        _heap[r.Index] = new java.lang.String
        {
            Value = str,
            HeapAddress = r.Index,
            JavaClass = State.Classes["java/lang/String"]
        };
        return r;
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
        var r = TakePlace();
        _heap[r.Index] = new Array<T>
        {
            HeapAddress = r.Index,
            Value = new T[length],
            JavaClass = State.Classes["java/lang/Array"]
        };
        return r;
    }

    public Reference AllocateArray<T>(T[] data) where T : struct
    {
        var r = TakePlace();
        _heap[r.Index] = new Array<T>
        {
            HeapAddress = r.Index,
            Value = data,
            JavaClass = State.Classes["java/lang/Array"]
        };
        return r;
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

    private Reference TakePlace()
    {
        lock (this)
        {
            var r = new Reference(_nextObjectId);
            _nextObjectId++;
            return r;
        }
    }
}