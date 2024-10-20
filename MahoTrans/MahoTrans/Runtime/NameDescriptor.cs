// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Utils;

namespace MahoTrans.Runtime;

/// <summary>
///     Two combined strings: name of the member and member's descriptor. Used to identify class members, i.e. fields and
///     methods.
/// </summary>
public readonly struct NameDescriptor : IEquatable<NameDescriptor>
{
    public readonly string Name;
    public readonly string Descriptor;

    public NameDescriptor(string name, string descriptor)
    {
        Name = name;
        Descriptor = descriptor;
    }

    public override string ToString()
    {
        if (Descriptor[0] == '(')
            return $"{Name}{Descriptor}";
        return $"{Descriptor}+{Name}";
    }

    public static implicit operator string(NameDescriptor descriptor) => descriptor.ToString();

    public bool Equals(NameDescriptor other)
    {
        return Name == other.Name && Descriptor == other.Descriptor;
    }

    public override bool Equals(object? obj)
    {
        return obj is NameDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)GetSnapshotHash();
    }

    public uint GetSnapshotHash()
    {
        return Name.GetSnapshotHash() ^ Descriptor.GetSnapshotHash();
    }

    public static bool operator ==(NameDescriptor left, NameDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NameDescriptor left, NameDescriptor right)
    {
        return !left.Equals(right);
    }

    #region Constants

    /// <summary>
    ///     Descriptor of "clinit" method.
    /// </summary>
    public static NameDescriptor ClassInit => new("<clinit>", "()V");

    /// <summary>
    ///     Descriptor of default constructor.
    /// </summary>
    public static NameDescriptor ObjectInit => new("<init>", "()V");

    #endregion
}
