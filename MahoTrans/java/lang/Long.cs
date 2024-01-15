// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Long : Object
{
    public const long MAX_VALUE = long.MaxValue;
    public const long MIN_VALUE = long.MinValue;

    public long Value;

    [InitMethod]
    public void Init(long value)
    {
        Value = value;
    }

    public double doubleValue() => Value;

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var l = Jvm.ResolveObject(obj);
        if (l is not Long ll)
            return false;
        return ll.Value == Value;
    }

    public float floatValue() => Value;

    public new int hashCode()
    {
        return (int)(uint)(((ulong)Value) ^ (((ulong)Value) >> 32));
    }

    public long longValue() => Value;

    public static long parseLong([String] Reference s)
    {
        if (!long.TryParse(Jvm.ResolveString(s), out var i))
           Jvm.Throw<NumberFormatException>();
        return i;
    }

    public static long parseLong([String] Reference str, int radix)
    {
        if (str.IsNull || radix < 2 || radix > 36)
            Jvm.Throw<NumberFormatException>();
        string s = Jvm.ResolveString(str);
        if (s.Length == 0)
            Jvm.Throw<NumberFormatException>();
        bool negative = s[0] == '-';
        if (negative && s.Length == 1)
            Jvm.Throw<NumberFormatException>();
        long max = MIN_VALUE / radix;
        long result = 0;
        int offset = negative ? 1 : 0;
        while (offset < s.Length)
        {
            int digit = Character.digit(s[offset++], radix);
            if (digit == -1 || max > result)
                Jvm.Throw<NumberFormatException>();
            long next = result * radix - digit;
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

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString());
    }

    [return: String]
    public static Reference toString(long l)
    {
        return Jvm.AllocateString(l.ToString());
    }

    [return: String]
    public static Reference toString(long l, int radix)
    {
        if (radix < 2 || radix > 36)
            radix = 10;
        if (l == 0)
            return Jvm.AllocateString("0");
        int count = 2;
        long j = l;
        bool negative = l < 0;
        if (!negative)
        {
            count = 1;
            j = -l;
        }
        while ((l /= radix) != 0)
            ++count;
        char[] buffer = new char[count];
        do
        {
            int ch = 0 - (int)(j % radix);
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
}