using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class NoClassDefFoundError : Error
{
    [InitMethod]
    public void Init([String] Reference msg)
    {
    }
}