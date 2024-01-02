// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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