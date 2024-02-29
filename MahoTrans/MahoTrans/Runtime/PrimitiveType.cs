// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime;

/// <summary>
///     Type of things on frame's stack or locals. Members starting with "is" are attributes. Their combination leads to
///     actual primitive type.
/// </summary>
[Flags]
public enum PrimitiveType : byte
{
    // size

    /// <summary>
    ///     This takes 4 bytes.
    /// </summary>
    Is32 = 0x1,

    /// <summary>
    ///     This takes 8 bytes.
    /// </summary>
    Is64 = 0x2,

    // type

    /// <summary>
    ///     This is a float-point number, i.e. float or double.
    /// </summary>
    IsFloat = 0x4,

    /// <summary>
    ///     This is an integer, 32 or 64 bit.
    /// </summary>
    IsInteger = 0x8,

    /// <summary>
    ///     This is a reference.
    /// </summary>
    IsReference = 0x10,

    /// <summary>
    ///     This is a pointer.
    /// </summary>
    IsSubroutinePointer = 0x20,

    // primitives

    /// <summary>
    ///     Int32 primitive.
    /// </summary>
    Int = Is32 | IsInteger,

    /// <summary>
    ///     Int64 primitive.
    /// </summary>
    Long = Is64 | IsInteger,

    /// <summary>
    ///     Float32 primitive.
    /// </summary>
    Float = Is32 | IsFloat,

    /// <summary>
    ///     Float64 primitive.
    /// </summary>
    Double = Is64 | IsFloat,

    /// <summary>
    ///     Reference primitive.
    /// </summary>
    Reference = Is32 | IsReference,

    /// <summary>
    ///     Pointer primitive.
    /// </summary>
    SubroutinePointer = Is32 | IsSubroutinePointer,
}