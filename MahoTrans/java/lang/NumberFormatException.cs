using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class NumberFormatException : IllegalArgumentException
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