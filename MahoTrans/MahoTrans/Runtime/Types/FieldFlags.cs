namespace MahoTrans.Runtime.Types;

[Flags]
public enum FieldFlags
{
    Public = 0x01,
    Private = 0x02,
    Protected = 0x04,
    Static = 0x08,
    Final = 0x10,
    Volatile = 0x40,
    Transient = 0x80,
    Synthetic = 0x1000,
    Enum = 0x4000
}