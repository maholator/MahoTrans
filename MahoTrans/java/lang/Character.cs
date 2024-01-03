// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Character : Object
{
    public char Value;

    public const int MAX_RADIX = 36;
    public const char MAX_VALUE = (char)65535;
    public const int MIN_RADIX = 2;
    public const char MIN_VALUE = (char)0;

    [InitMethod]
    public void Init(char c) => Value = c;

    public char charValue() => Value;

    public static int digit(char ch, int radix)
    {
        var val = ch - '0';
        if (val < 0) return -1;
        if (radix <= 10)
        {
            if (val < radix)
                return val;
            return -1;
        }

        if (val < 10)
            return val;
        val = char.ToUpper(ch) - 'A';
        if (val < 0)
            return -1;
        val += 10;
        if (val < radix)
            return val;
        return -1;
    }

    public static char toLowerCase(char ch) => char.ToLower(ch);

    public static char toUpperCase(char ch) => char.ToUpper(ch);

    public static bool isLowerCase(char ch) => char.IsLower(ch);

    public static bool isUpperCase(char ch) => char.IsUpper(ch);

    public static bool isDigit(char ch) => char.IsDigit(ch);

    [return: String]
    public Reference toString() => Jvm.AllocateString($"{Value}");

    public new int hashCode() => Value;

    public new bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Character c)
            return false;
        return c.Value == Value;
    }
}