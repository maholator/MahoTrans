namespace MahoTrans.Runtime;

public readonly struct Instruction : IEquatable<Instruction>
{
    public readonly int Offset;
    public readonly JavaOpcode Opcode;
    public readonly byte[] Args;

    public Instruction(int offset, JavaOpcode opcode, byte[] args)
    {
        Opcode = opcode;
        Args = args;
        Offset = offset;
    }

    public Instruction(int offset, JavaOpcode opcode)
    {
        Opcode = opcode;
        Offset = offset;
        Args = Array.Empty<byte>();
    }

    public Instruction(JavaOpcode opcode, byte[] args)
    {
        Opcode = opcode;
        Offset = 0;
        Args = args;
    }

    public Instruction(JavaOpcode opcode)
    {
        Opcode = opcode;
        Offset = 0;
        Args = Array.Empty<byte>();
    }

    public override string ToString()
    {
        if (Args.Length == 0)
            return Opcode.ToString();

        return $"{Opcode} {string.Join(',', Args)}";
    }

    public bool Equals(Instruction other)
    {
        return Offset == other.Offset && Opcode == other.Opcode && Args.Equals(other.Args);
    }

    public override bool Equals(object? obj)
    {
        return obj is Instruction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, (int)Opcode, Args);
    }

    public static bool operator ==(Instruction left, Instruction right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Instruction left, Instruction right)
    {
        return !left.Equals(right);
    }
}