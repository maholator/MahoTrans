// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Double : Object
{
    public const double MAX_VALUE = double.MaxValue;
    public const double MIN_VALUE = double.MinValue;
    public const double NaN = double.NaN;
    public const double POSITIVE_INFINITY = double.PositiveInfinity;
    public const double NEGATIVE_INFINITY = double.NegativeInfinity;

    public double Value;

    [InitMethod]
    public void Init(double value)
    {
        Value = value;
    }

    public static long doubleToLongBits(double value) => BitConverter.DoubleToInt64Bits(value);

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

    public sbyte byteValue() => (sbyte) Value;
    public short shortValue() => (short) Value;
    public int intValue() => (int) Value;
    public long longValue() => (long) Value;
    public double doubleValue() => Value;
    public float floatValue() => (float) Value;

    public new int hashCode()
    {
        ulong v = (ulong)BitConverter.DoubleToInt64Bits(Value);
        return (int)(v ^ (v >> 32));
    }

    public static double longBitsToDouble(long bits) => BitConverter.Int64BitsToDouble(bits);

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

    [return: JavaType(typeof(Double))]
    public static Reference valueOf([String] Reference str)
    {
        var i = Jvm.AllocateObject<Double>();
        i.Init(parseDouble(str));
        return i.This;
    }

    public bool isInfinite() => isInfinite(Value);

    public static bool isInfinite(double f) => f == POSITIVE_INFINITY || f == NEGATIVE_INFINITY;

    public bool isNaN() => isNaN(Value);

    public static bool isNaN(double f) => double.IsNaN(f);
}