using MahoTrans.Native;

namespace java.lang;

public class Float : Object
{
    public float Value;

    [InitMethod]
    public void Init(float v) => Value = v;

    [InitMethod]
    public void Init(double v) => Value = (float)v;

    public float floatValue() => Value;

    public double doubleValue() => Value;

    static int floatToIntBits(float v) => BitConverter.SingleToInt32Bits(v);
}