using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.io;

public class EOFException : IOException
{
    [InitMethod]
    public new void Init([String] Reference msg)
    {
    }

    [InitMethod]
    public new void Init()
    {
    }
}