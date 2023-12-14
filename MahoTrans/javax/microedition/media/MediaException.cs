using MahoTrans.Native;
using MahoTrans.Runtime;
using Exception = java.lang.Exception;

namespace javax.microedition.media;

public class MediaException : Exception
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