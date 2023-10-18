using MahoTrans.Native;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Displayable : Object
{
    [JavaIgnore] public DisplayableHandle Handle;

    [InitMethod]
    public override void Init()
    {
        Handle = Toolkit.Display.Register(this);
    }

    public int getWidth() => Toolkit.Display.GetWidth(Handle);

    public int getHeight() => Toolkit.Display.GetHeight(Handle);
}