namespace MahoTrans.Runtime.Types;

[Flags]
public enum ClassFlags : short
{
    Public = 0x01,
    Final = 0x10,
    Super = 0x20,
    Interface = 0x0200,
    Abstract = 0x0400,
    Synthetic = 0x1000,
    Annotation = 0x2000,
    Enum = 0x4000
}