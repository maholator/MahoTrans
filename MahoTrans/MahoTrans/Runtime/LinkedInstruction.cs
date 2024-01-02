namespace MahoTrans.Runtime;

public readonly struct LinkedInstruction
{
    public readonly MTOpcode Opcode;
    public readonly ushort ShortData;
    public readonly int IntData;
    public readonly object Data;

    // opcode _ short short . int int int int . data data data data . data data data data

    public LinkedInstruction(MTOpcode opcode, ushort shortData, int intData, object data)
    {
        Opcode = opcode;
        ShortData = shortData;
        IntData = intData;
        Data = data;
    }

    public LinkedInstruction(MTOpcode opcode, int intData)
    {
        Opcode = opcode;
        ShortData = 0;
        IntData = intData;
        Data = null!;
    }

    public LinkedInstruction(MTOpcode opcode)
    {
        Opcode = opcode;
        ShortData = 0;
        IntData = 0;
        Data = null!;
    }

    public override string ToString()
    {
        return $"{Opcode} {Data}";
    }
}