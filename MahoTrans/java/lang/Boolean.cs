// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Boolean : Object
{
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