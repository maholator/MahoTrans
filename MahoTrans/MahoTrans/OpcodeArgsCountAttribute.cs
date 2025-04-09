// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

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
