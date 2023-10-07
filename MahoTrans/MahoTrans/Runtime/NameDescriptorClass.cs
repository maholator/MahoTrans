using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public readonly struct NameDescriptorClass : IEquatable<NameDescriptorClass>
{
    public readonly NameDescriptor Descriptor;
    public readonly string ClassName;

    /// <summary>
    /// Constructs arbitrary reference to member.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="descriptor">Java descriptor.</param>
    /// <param name="className">Containing class name.</param>
    public NameDescriptorClass(string name, string descriptor, string className)
    {
        Descriptor = new NameDescriptor(name, descriptor);
        ClassName = className;
    }

    /// <summary>
    /// Constructs arbitrary reference to member.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="descriptor">Java descriptor.</param>
    /// <param name="class">Containing class.</param>
    public NameDescriptorClass(string name, string descriptor, Type @class)
    {
        Descriptor = new NameDescriptor(name, descriptor);
        ClassName = @class.ToJavaName();
    }

    /// <summary>
    /// Constructs reference to a field.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="className">Containing class name.</param>
    public NameDescriptorClass(string name, Type type, string className)
    {
        Descriptor = new NameDescriptor(name, type.ToJavaDescriptor());
        ClassName = className;
    }

    /// <summary>
    /// Constructs reference to a field.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="class">Containing class.</param>
    public NameDescriptorClass(string name, Type type, Type @class)
    {
        Descriptor = new NameDescriptor(name, type.ToJavaDescriptor());
        ClassName = @class.ToJavaName();
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