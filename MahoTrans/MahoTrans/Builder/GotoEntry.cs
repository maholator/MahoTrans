// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace MahoTrans.Builder;

public class GotoEntry : IBuilderEntry
{
    public readonly JavaOpcode Opcode;
    public readonly JavaLabel Label;

    public GotoEntry(JavaOpcode opcode, JavaLabel label)
    {
        Opcode = opcode;
        Label = label;
    }

    public int ArgsLength => 2;
}
