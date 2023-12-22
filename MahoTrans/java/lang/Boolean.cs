using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Boolean : Object
{
    public bool Value;

    [InitMethod]
    public void Init(bool v)
    {
        Value = v;
    }

    public bool booleanValue() => Value;

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value ? "true" : "false");
    }
}