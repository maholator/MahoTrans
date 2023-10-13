using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Integer : Object
{
    private int Value;

    [InitMethod]
    public void Init(int value)
    {
        Value = value;
    }

    public sbyte byteValue() => (sbyte)Value;
    public short shortValue() => (short)Value;
    public int intValue() => Value;
    public long longValue() => Value;


    public int hashCode()
    {
        return Value;
    }

    public bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Heap.ResolveObject(obj);
        if (o is not Integer ii)
            return false;
        return ii.Value == Value;
    }


    [return: String]
    public Reference toString()
    {
        return Heap.AllocateString(Value.ToString());
    }

    [return: String]
    public static Reference toString(int i)
    {
        return Heap.AllocateString(i.ToString());
    }


    [return: String]
    public static Reference toHexString(int i)
    {
        return Heap.AllocateString(Convert.ToString(i, 16));
    }

    public static int parseInt([String] Reference str) => int.Parse(Heap.ResolveString(str));

    public static int parseInt([String] Reference str, int radix) => Convert.ToInt32(Heap.ResolveString(str), radix);
}