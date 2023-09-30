using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class RuntimeException : Exception
{
    [InitMethod]
    public void Init([String] Reference msg)
    {
    }
}