using MahoTrans.Native;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Displayable : Object
{
    [JavaIgnore] public DisplayableDescriptor Handle;

    [InitMethod]
    public override void Init()
    {
        Handle = Toolkit.Display.Register(this);
    }

    public int getWidth() => Toolkit.Display.Resolve(Handle).Width;

    public int getHeight() => Toolkit.Display.Resolve(Handle).Height;
}