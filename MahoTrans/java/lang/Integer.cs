// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
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
        if (radix < 2 || radix > 36)
            radix = 10;
        if (i == 0)
            return Jvm.AllocateString("0");
        int count = 2;
        int j = i;
        bool negative = i < 0;
        if (!negative)
        {
            count = 1;
            j = -i;
        }

        while ((i /= radix) != 0)
            ++count;
        char[] buffer = new char[count];
        do
        {
            int ch = 0 - j % radix;
            if (ch > 9)
                ch = ch - 10 + 97;
            else
                ch += 48;
            buffer[--count] = (char)ch;
        } while ((j /= radix) != 0);

        if (negative)
            buffer[0] = '-';
        return Jvm.AllocateString(new string(buffer, 0, buffer.Length));
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
        if (str.IsNull || radix < 2 || radix > 36)
            Jvm.Throw<NumberFormatException>();
        string s = Jvm.ResolveString(str);
        if (s.Length == 0)
            Jvm.Throw<NumberFormatException>();
        bool negative = s[0] == '-';
        if (negative && s.Length == 1)
            Jvm.Throw<NumberFormatException>();
        int max = MIN_VALUE / radix;
        int result = 0;
        int offset = negative ? 1 : 0;
        while (offset < s.Length)
        {
            int digit = Character.digit(s[offset++], radix);
            if (digit == -1 || max > result)
                Jvm.Throw<NumberFormatException>();
            int next = result * radix - digit;
            if (next > result)
                Jvm.Throw<NumberFormatException>();
            result = next;
        }

        if (!negative)
        {
            result = -result;
            if (result < 0)
                Jvm.Throw<NumberFormatException>();
        }

        return result;
    }

    [return: JavaType(typeof(Integer))]
    public static Reference valueOf([String] Reference str)
    {
        var i = Jvm.Allocate<Integer>();
        i.Init(parseInt(str));
        return i.This;
    }

    [return: JavaType(typeof(Integer))]
    public static Reference valueOf([String] Reference str, int radix)
    {
        var i = Jvm.Allocate<Integer>();
        i.Init(parseInt(str, radix));
        return i.This;
    }
}
