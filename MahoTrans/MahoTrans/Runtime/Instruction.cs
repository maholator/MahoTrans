// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;

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

    public Instruction(int offset, JavaOpcode opcode, byte arg)
    {
        Opcode = opcode;
        Offset = offset;
        Args = new[] { arg };
    }

    public Instruction(JavaOpcode opcode, byte[] args)
    {
        Opcode = opcode;
        Offset = 0;
        Debug.Assert(args != null, "Args must be not null!");
        Args = args;
    }

    public Instruction(JavaOpcode opcode)
    {
        Opcode = opcode;
        Offset = 0;
        Args = Array.Empty<byte>();
    }

    public Instruction(JavaOpcode opcode, byte arg)
    {
        Opcode = opcode;
        Offset = 0;
        Args = new[] { arg };
    }

    public override string ToString()
    {
        if (Args == null || Args.Length == 0)
            return Opcode.ToString();

        return $"{Opcode} {string.Join(',', Args)}";
    }

    public bool Equals(Instruction other)
    {
        return Offset == other.Offset && Opcode == other.Opcode && Args.SequenceEqual(other.Args);
    }

    public override bool Equals(object? obj)
    {
        return obj is Instruction other && Equals(other);
    }

    public override int GetHashCode()
    {
        uint argsHash = (uint)Opcode ^ (uint)Offset;
        foreach (var arg in Args)
            argsHash ^= arg;

        return (int)argsHash;
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
