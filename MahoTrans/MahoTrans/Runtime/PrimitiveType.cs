namespace MahoTrans.Runtime;

[Flags]
public enum PrimitiveType
{
    // size
    IsSingle = 0x1,
    IsDouble = 0x2,
    
    // type
    IsFloat = 0x4,
    IsInt = 0x8,
    IsReference = 0x10,
    IsSubroutinePointer = 0x20,
    
    // primitives
    Int = IsSingle | IsInt,
    Long = IsDouble | IsInt,
    Float = IsSingle | IsFloat,
    Double = IsDouble | IsFloat,
    Reference = IsSingle | IsReference,
    SubroutinePointer = IsSingle | IsSubroutinePointer,
    
}