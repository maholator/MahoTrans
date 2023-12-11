namespace MahoTrans.Runtime;

public readonly struct LinkedInstruction
{
    public readonly JavaOpcode Opcode;
    public readonly int IntData;
    public readonly object Data;

    // opcode _ _ _ . int int int int . data data data data . data data data data

    public LinkedInstruction(JavaOpcode opcode, int intData, object data)
    {
        Opcode = opcode;
        IntData = intData;
        Data = data;
    }

    public override string ToString()
    {
        return $"{Opcode} {Data}";
    }
}