using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Boolean : Object
{
    private bool _value;

    [InitMethod]
    public void Init(bool v)
    {
        _value = v;
    }

    [return: String]
    public Reference toString()
    {
        return Heap.AllocateString(_value ? "true" : "false");
    }
}