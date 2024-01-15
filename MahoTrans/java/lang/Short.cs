// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Short : Object
{
    public const short MAX_VALUE = 32767;
    public const short MIN_VALUE = -32768;

    [JavaIgnore] public short Value;

    [InitMethod]
    public void Init(short v)
    {
        Value = v;
    }

    public short shortValue() => Value;

    public static short parseShort([String] Reference str)
    {
        if (!int.TryParse(Jvm.ResolveString(str), out var i))
            Jvm.Throw<NumberFormatException>();

        if (i < short.MinValue || i > short.MaxValue)
            Jvm.Throw<NumberFormatException>();

        return (short)i;
    }

    public new bool equals(Reference r)
    {
        if (r.IsNull)
            return false;

        var obj = Jvm.ResolveObject(r);

        if (obj is Short s)
        {
            return s.Value == Value;
        }

        return false;
    }

    //TODO proper checks
    public static short parseShort([String] Reference str, int radix)
    {
        try
        {
            return Convert.ToInt16(Jvm.ResolveString(str), radix);
        }
        catch
        {
            Jvm.Throw<NumberFormatException>();
        }
        return 0;
    }
}