using System.Globalization;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Double : Object
{
    public double Value;

    [InitMethod]
    public void Init(double value)
    {
        Value = value;
    }

    public sbyte byteValue() => (sbyte)Value;

    public static long doubleToLongBits(double value) => BitConverter.DoubleToInt64Bits(value);

    public double doubleValue() => Value;

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Double ii)
            return false;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return ii.Value == Value;
    }

    public float floatValue() => (float)Value;

    public new int hashCode()
    {
        ulong v = (ulong)BitConverter.DoubleToInt64Bits(Value);
        return (int)(v ^ (v >> 32));
    }

    public int intValue() => (int)Value;

    public static double longBitsToDouble(long bits) => BitConverter.Int64BitsToDouble(bits);

    public long longValue() => (long)Value;

    public short shortValue() => (short)Value;

    [return: String]
    public static Reference toString(double d)
    {
        return Jvm.AllocateString(d.ToString(CultureInfo.InvariantCulture));
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString(CultureInfo.InvariantCulture));
    }

    public static double parseDouble([String] Reference str)
    {
        var s = Jvm.ResolveString(str);
        if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            Jvm.Throw<NumberFormatException>();
        return d;
    }
}