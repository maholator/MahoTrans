// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Boolean : Object
{
    [ClassInit]
    public static void Clinit()
    {
        var t = Jvm.AllocateObject<Boolean>();
        t.Init(true);
        NativeStatics.True = t.This;

        var f = Jvm.AllocateObject<Boolean>();
        f.Init(true);
        NativeStatics.False = f.This;
    }

    public bool Value;

    [InitMethod]
    public void Init(bool v)
    {
        Value = v;
    }

    public new int hashCode()
    {
        return Value ? 1231 : 1237;
    }

    public bool booleanValue() => Value;

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value ? "true" : "false");
    }
}