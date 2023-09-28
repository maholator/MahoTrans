namespace MahoTrans.Runtime;

public readonly struct NameDescriptorClass : IEquatable<NameDescriptorClass>
{
    public readonly NameDescriptor Descriptor;
    public readonly string ClassName;

    public NameDescriptorClass(string name, string descriptor, string className)
    {
        Descriptor = new NameDescriptor(name, descriptor);
        ClassName = className;
    }

    public NameDescriptorClass(NameDescriptor descriptor, string className)
    {
        Descriptor = descriptor;
        ClassName = className;
    }

    public override string ToString()
    {
        return $"{ClassName} {Descriptor}";
    }

    public bool Equals(NameDescriptorClass other)
    {
        return Descriptor.Equals(other.Descriptor) && ClassName == other.ClassName;
    }

    public override bool Equals(object? obj)
    {
        return obj is NameDescriptorClass other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Descriptor, ClassName);
    }

    public static bool operator ==(NameDescriptorClass left, NameDescriptorClass right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NameDescriptorClass left, NameDescriptorClass right)
    {
        return !left.Equals(right);
    }
}