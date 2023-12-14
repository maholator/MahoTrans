using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class ArrayIndexOutOfBoundsException : IndexOutOfBoundsException
{
    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    [InitMethod]
    public new void Init([String] Reference message)
    {
        base.Init(message);
    }
}