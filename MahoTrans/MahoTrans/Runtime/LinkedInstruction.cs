namespace MahoTrans.Runtime;

public readonly struct LinkedInstruction
{
    public readonly JavaOpcode Opcode;
    public readonly ushort ShortData;
    public readonly int IntData;
    public readonly object Data;

    // opcode _ short short . int int int int . data data data data . data data data data

    public LinkedInstruction(JavaOpcode opcode, ushort shortData, int intData, object data)
    {
        Opcode = opcode;
        ShortData = shortData;
        IntData = intData;
        Data = data;
    }

    public override string ToString()
    {
        return $"{Opcode} {Data}";
    }
}