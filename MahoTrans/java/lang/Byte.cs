// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Byte : Object
{
    public const sbyte MAX_VALUE = 127;
    public const sbyte MIN_VALUE = -128;

    public sbyte Value;

    [InitMethod]
    public void Init(sbyte b) => Value = b;

    public sbyte byteValue() => Value;

    public static sbyte parseByte([String] Reference str)
    {
        if (!int.TryParse(Jvm.ResolveString(str), out var i))
            Jvm.Throw<NumberFormatException>();

        if (i < -128 || i > 127)
            Jvm.Throw<NumberFormatException>();

        return (sbyte)i;
    }

    public static sbyte parseByte([String] Reference str, int radix)
    {
        try
        {
            var i = Convert.ToInt32(Jvm.ResolveString(str), radix);
            if (i < -128 || i > 127)
                Jvm.Throw<NumberFormatException>();
            return (sbyte)i;
        }
        catch
        {
            Jvm.Throw<NumberFormatException>();
        }

        return 0;
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString());
    }

    public new int hashCode() => Value;

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Byte ii)
            return false;
        return ii.Value == Value;
    }
}
