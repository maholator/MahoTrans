using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Error : Throwable
{
    [InitMethod]
    public new void Init([String] Reference message)
    {
        base.Init(message);
    }
}