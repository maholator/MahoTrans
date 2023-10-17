using MahoTrans.Native;

namespace java.lang;

public class Byte : Object
{
    public sbyte Value;

    [InitMethod]
    public void Init(sbyte b) => Value = b;

    public sbyte byteValue() => Value;
}