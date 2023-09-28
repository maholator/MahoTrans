using MahoTrans.Native;

namespace java.lang;

public class Boolean : Object
{
    private bool _value;

    [InitMethod]
    public void Init(bool v)
    {
        _value = v;
    }
}