// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;

namespace MahoTrans.Runtime.Types;

/// <summary>
///     Represents a field inside JVM type.
/// </summary>
public class Field
{
    public readonly FieldFlags Flags;
    public readonly string ClassName;
    public readonly NameDescriptor Descriptor;

    /// <summary>
    ///     Reference to field metadata.
    /// </summary>
    public FieldInfo? NativeField;

    /// <summary>
    ///     Bridge method to get field value. Pops object reference from the frame and pushes the value back. No extra actions
    ///     are required from the interpreter.
    /// </summary>
    public Action<Frame>? GetValue;

    /// <summary>
    ///     Bridge method to get field value. Pops object reference and value from the frame. No extra actions are required
    ///     from the interpreter.
    /// </summary>
    public Action<Frame>? SetValue;

    public JavaAttribute[] Attributes = Array.Empty<JavaAttribute>();

    public Field(NameDescriptor descriptor, FieldFlags flags, string className)
    {
        Flags = flags;
        ClassName = className;
        Descriptor = descriptor;
    }

    public bool IsStatic => Flags.HasFlag(FieldFlags.Static);

    public override string ToString() => Descriptor.ToString();

    public override int GetHashCode() => Descriptor.GetHashCode();

    public uint GetSnapshotHash() => Descriptor.GetSnapshotHash();
}
