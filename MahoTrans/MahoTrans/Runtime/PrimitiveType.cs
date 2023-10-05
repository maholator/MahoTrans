namespace MahoTrans.Runtime;

[Flags]
public enum PrimitiveType : byte
{
    // size

    /// <summary>
    /// This takes 4 bytes.
    /// </summary>
    IsSingle = 0x1,

    /// <summary>
    /// This takes 8 bytes.
    /// </summary>
    IsDouble = 0x2,

    // type

    /// <summary>
    /// This is a float-point number, i.e. float or double.
    /// </summary>
    IsFloat = 0x4,

    /// <summary>
    /// This is an integer, 32 or 64 bit.
    /// </summary>
    IsInt = 0x8,

    /// <summary>
    /// This is a reference.
    /// </summary>
    IsReference = 0x10,

    /// <summary>
    /// This is a pointer.
    /// </summary>
    IsSubroutinePointer = 0x20,

    // primitives

    /// <summary>
    /// Int32 primitive.
    /// </summary>
    Int = IsSingle | IsInt,

    /// <summary>
    /// Int64 primitive.
    /// </summary>
    Long = IsDouble | IsInt,

    /// <summary>
    /// Float32 primitive.
    /// </summary>
    Float = IsSingle | IsFloat,

    /// <summary>
    /// Float64 primitive.
    /// </summary>
    Double = IsDouble | IsFloat,

    /// <summary>
    /// Reference primitive.
    /// </summary>
    Reference = IsSingle | IsReference,

    /// <summary>
    /// Pointer primitive.
    /// </summary>
    SubroutinePointer = IsSingle | IsSubroutinePointer,
}