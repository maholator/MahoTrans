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