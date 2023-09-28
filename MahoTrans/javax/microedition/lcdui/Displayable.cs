using MahoTrans.Native;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Displayable : Object
{
    [JavaIgnore] public IDisplayable Handle = null!;

    [InitMethod]
    public override void Init()
    {
        Handle = Toolkit.Display.Register(this);
    }

    public int getWidth() => Handle.Width;

    public int getHeight() => Handle.Height;
}