namespace MahoTrans.Runtime.Types;

public class JavaAttribute
{
    public readonly string Type;
    public byte[] Data = Array.Empty<byte>();

    public JavaAttribute(string type)
    {
        Type = type;
    }
}