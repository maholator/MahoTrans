using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class RuntimeException : Exception
{
    [InitMethod]
    public new void Init([String] Reference msg)
    {
        base.Init(msg);
    }
}