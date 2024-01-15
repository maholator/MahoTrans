// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Integer : Object
{
    public const int MAX_VALUE = int.MaxValue;
    public const int MIN_VALUE = int.MinValue;

    public int Value;

    [InitMethod]
    public void Init(int value)
    {
        Value = value;
    }

    public sbyte byteValue() => (sbyte)Value;
    public short shortValue() => (short)Value;
    public int intValue() => Value;
    public long longValue() => Value;
    public double doubleValue() => Value;
    public float floatValue() => Value;

    public new int hashCode()
    {
        return Value;
    }

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Integer ii)
            return false;
        return ii.Value == Value;
    }


    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString());
    }

    [return: String]
    public static Reference toString(int i)
    {
        return Jvm.AllocateString(i.ToString());
    }

    [return: String]
    public static Reference toString(int i, int radix)
    {
        return Jvm.AllocateString(Convert.ToString(i, radix));
    }

    [return: String]
    public static Reference toBinaryString(int i)
    {
        return Jvm.AllocateString(Convert.ToString(i, 2));
    }

    [return: String]
    public static Reference toOctalString(int i)
    {
        return Jvm.AllocateString(Convert.ToString(i, 8));
    }

    [return: String]
    public static Reference toHexString(int i)
    {
        return Jvm.AllocateString(Convert.ToString(i, 16));
    }

    public static int parseInt([String] Reference str)
    {
        if (!int.TryParse(Jvm.ResolveString(str), out var i))
            Jvm.Throw<NumberFormatException>();

        return i;
    }

    public static int parseInt([String] Reference str, int radix)
    {
        try
        {
            return Convert.ToInt32(Jvm.ResolveString(str), radix);
        }
        catch
        {
            Jvm.Throw<NumberFormatException>();
        }
        return 0;
    }

    [return: JavaType(typeof(Integer))]
    public static Reference valueOf([String] Reference str)
    {
        var i = Jvm.AllocateObject<Integer>();
        i.Init(parseInt(str));
        return i.This;
    }

    [return: JavaType(typeof(Integer))]
    public static Reference valueOf([String] Reference str, int radix)
    {
        var i = Jvm.AllocateObject<Integer>();
        i.Init(parseInt(str, radix));
        return i.This;
    }
}