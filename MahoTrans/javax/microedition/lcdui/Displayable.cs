using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkits;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Displayable : Object
{
    [JavaIgnore] public DisplayableHandle Handle;

    [String] public Reference Title = 0;

    [JavaType(typeof(CommandListener))] public Reference Listener;

    [JavaIgnore] public List<Reference> Commands = new();

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

    public void addCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd.IsNull)
            Jvm.Throw<NullPointerException>();
        if (Commands.Contains(cmd))
            return;
        Commands.Add(cmd);
        //TODO notify toolkit
    }

    public void removeCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd.IsNull)
            return;
        Commands.Remove(cmd);
        //TODO notify toolkit
    }

    public void setCommandListener([JavaType(typeof(CommandListener))] Reference l)
    {
        Listener = l;
    }

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(Commands);
        base.AnnounceHiddenReferences(queue);
    }
}