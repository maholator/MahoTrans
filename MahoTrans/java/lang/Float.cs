// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Float : Object
{
    public const float MAX_VALUE = float.MaxValue;
    public const float MIN_VALUE = float.MinValue;
    public const float NaN = float.NaN;
    public const float POSITIVE_INFINITY = float.PositiveInfinity;
    public const float NEGATIVE_INFINITY = float.NegativeInfinity;

    public float Value;

    [InitMethod]
    public void Init(float v) => Value = v;

    [InitMethod]
    public void Init(double v) => Value = (float)v;

    public sbyte byteValue() => (sbyte) Value;
    public short shortValue() => (short) Value;
    public long intValue() => (int) Value;
    public long longValue() => (long) Value;
    public double doubleValue() => Value;
    public float floatValue() => Value;

    public static int floatToIntBits(float v) => BitConverter.SingleToInt32Bits(v);

    public static float intBitsToFloat(int v) => BitConverter.Int32BitsToSingle(v);

    public new int hashCode() => floatToIntBits(Value);

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Float ii)
            return false;
        return ii.Value == Value;
    }

    [return: String]
    public static Reference toString(float f)
    {
        return Jvm.AllocateString(f.ToString(CultureInfo.InvariantCulture));
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString(CultureInfo.InvariantCulture));
    }

    public static float parseFloat([String] Reference str)
    {
        var s = Jvm.ResolveString(str);
        if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
            Jvm.Throw<NumberFormatException>();
        return f;
    }

    public bool isInfinite() => isInfinite(Value);

    public static bool isInfinite(float f) => f == POSITIVE_INFINITY || f == NEGATIVE_INFINITY;

    public bool isNaN() => isNaN(Value);

    public static bool isNaN(float f) => float.IsNaN(f);

    [return: JavaType(typeof(Float))]
    public Reference valueOf([String] Reference str)
    {
        var i = Jvm.AllocateObject<Float>();
        i.Init(parseFloat(str));
        return i.This;
    }
}