namespace MahoTrans.Runtime;

public readonly struct LinkedInstruction
{
    public readonly JavaOpcode Opcode;
    public readonly object Data;

    public LinkedInstruction(JavaOpcode opcode, object data)
    {
        Opcode = opcode;
        Data = data;
    }

    public override string ToString()
    {
        return $"{Opcode} {Data}";
    }
}