// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Builder;

public class InstructionEntry : IBuilderEntry
{
    public readonly Instruction Instruction;

    public InstructionEntry(Instruction instruction)
    {
        Instruction = instruction;
    }

    public int ArgsLength => Instruction.Args.Length;

    public static implicit operator Instruction(InstructionEntry entry)
    {
        return entry.Instruction;
    }

    public static implicit operator InstructionEntry(Instruction instruction)
    {
        return new InstructionEntry(instruction);
    }
}
