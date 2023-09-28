using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Long : Object
{
    private long Value;

    [InitMethod]
    public void Init(long value)
    {
        Value = value;
    }

    public double doubleValue() => Value;

    public bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var l = Heap.ResolveObject(obj);
        if (l is not Long ll)
            return false;
        return ll.Value == Value;
    }

    public float floatValue() => Value;

    public int hashCode()
    {
        return (int)(uint)(((ulong)Value) ^ (((ulong)Value) >> 32));
    }

    public long longValue() => Value;

    public static long parseLong([String] Reference s) => long.Parse(Heap.ResolveString(s));

    public static long parseLong([String] Reference s, int radix)
    {
        return Convert.ToInt64(Heap.ResolveString(s), radix);
    }

    [return: String]
    public Reference toString()
    {
        return Heap.AllocateString(Value.ToString());
    }

    [return: String]
    public static Reference toString(long l)
    {
        return Heap.AllocateString(l.ToString());
    }

    [return: String]
    public static Reference toString(long i, int radix)
    {
        return Heap.AllocateString(Convert.ToString(i, radix));
    }
}