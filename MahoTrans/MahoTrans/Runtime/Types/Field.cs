using System.Reflection;

namespace MahoTrans.Runtime.Types;

/// <summary>
/// Represents a field inside JVM type.
/// </summary>
public class Field
{
    public readonly FieldFlags Flags;
    public readonly NameDescriptor Descriptor;

    /// <summary>
    /// Reference to field metadata.
    /// </summary>
    public FieldInfo NativeField = null!;

    /// <summary>
    /// Bridge method to get field value. Pops object reference from the frame and pushes the value back. No extra actions are required from the interpreter.
    /// </summary>
    public Action<Frame>? GetValue;

    /// <summary>
    /// Bridge method to get field value. Pops object reference and value from the frame. No extra actions are required from the interpreter.
    /// </summary>
    public Action<Frame>? SetValue;
    public JavaAttribute[] Attributes = Array.Empty<JavaAttribute>();

    public Field(NameDescriptor descriptor, FieldFlags flags)
    {
        Flags = flags;
        Descriptor = descriptor;
    }

    public override string ToString() => Descriptor.ToString();
}