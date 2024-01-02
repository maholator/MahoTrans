// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Float : Object
{
    public float Value;

    [InitMethod]
    public void Init(float v) => Value = v;

    [InitMethod]
    public void Init(double v) => Value = (float)v;

    public float floatValue() => Value;

    public double doubleValue() => Value;

    static int floatToIntBits(float v) => BitConverter.SingleToInt32Bits(v);

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
}