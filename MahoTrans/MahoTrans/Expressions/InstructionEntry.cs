using MahoTrans.Runtime;

namespace MahoTrans.Expressions;

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