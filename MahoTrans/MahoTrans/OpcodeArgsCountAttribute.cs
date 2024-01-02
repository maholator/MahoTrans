// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans;

[AttributeUsage(AttributeTargets.Field)]
public class OpcodeArgsCountAttribute : Attribute
{
    public int ArgsCount;

    public OpcodeArgsCountAttribute(int argsCount)
    {
        ArgsCount = argsCount;
    }
}