using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Throwable : Object
{
    [InitMethod]
    public void Init([String] Reference message)
    {
    }
    public void printStackTrace()
    {
        Jvm.Toolkit.System.PrintException(this);
    }
}