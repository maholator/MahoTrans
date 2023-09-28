namespace MahoTrans.Runtime;

public readonly struct NameDescriptor : IEquatable<NameDescriptor>
{
    public readonly string Name;
    public readonly string Descriptor;

    public NameDescriptor(string name, string descriptor)
    {
        Name = name;
        Descriptor = descriptor;
    }
    public override string ToString() => $"{Descriptor}+{Name}";

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
        return HashCode.Combine(Name, Descriptor);
    }

    public static bool operator ==(NameDescriptor left, NameDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NameDescriptor left, NameDescriptor right)
    {
        return !left.Equals(right);
    }
}