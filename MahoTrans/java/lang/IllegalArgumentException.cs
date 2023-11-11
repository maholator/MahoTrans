using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class IllegalArgumentException : RuntimeException
{
    [InitMethod]
    public void Init()
    {
    }

    [InitMethod]
    public new void Init([String] Reference msg)
    {
        base.Init(msg);
    }
}