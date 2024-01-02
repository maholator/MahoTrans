// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Long : Object
{
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

    public static long parseLong([String] Reference s) => long.Parse(Jvm.ResolveString(s));

    public static long parseLong([String] Reference s, int radix)
    {
        return Convert.ToInt64(Jvm.ResolveString(s), radix);
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
    public static Reference toString(long i, int radix)
    {
        return Jvm.AllocateString(Convert.ToString(i, radix));
    }
}