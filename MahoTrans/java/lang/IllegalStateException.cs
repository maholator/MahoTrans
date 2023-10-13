using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class IllegalStateException : RuntimeException
{
    [InitMethod]
    public void Init()
    {
    }

    [InitMethod]
    public void Init([String] Reference msg)
    {
    }
}