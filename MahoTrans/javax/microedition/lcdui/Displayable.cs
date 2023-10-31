using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Displayable : Object
{
    [JavaIgnore] public DisplayableHandle Handle;

    [String] public Reference Title = 0;

    [InitMethod]
    public override void Init()
    {
        Handle = Toolkit.Display.Register(this);
    }

    public int getWidth() => Toolkit.Display.GetWidth(Handle);

    public int getHeight() => Toolkit.Display.GetHeight(Handle);

    public void setTitle([String] Reference title)
    {
        Title = title;
        Toolkit.Display.SetTitle(Handle, Jvm.ResolveString(title));
    }

    [return: String]
    public Reference getTitle() => Title;

    public bool isShown()
    {
        var c = Toolkit.Display.GetCurrent();
        if (c.HasValue)
            return Handle == c.Value;
        return false;
    }
}