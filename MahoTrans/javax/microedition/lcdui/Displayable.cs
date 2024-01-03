// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

    [JavaType(typeof(Ticker))] public Reference Ticker;

    [JavaIgnore] public List<Reference> Commands = new();

    [InitMethod]
    public override void Init()
    {
        base.Init();
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
        var c = Toolkit.Display.Current;
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
        Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
    }

    public void removeCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd.IsNull)
            return;
        Commands.Remove(cmd);
        Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
    }

    public void setCommandListener([JavaType(typeof(CommandListener))] Reference l)
    {
        Listener = l;
    }

    public void setTicker([JavaType(typeof(Ticker))] Reference ticker)
    {
        Ticker = ticker;
        Toolkit.Display.TickerUpdated();
    }

    [return: JavaType(typeof(Ticker))]
    public Reference getTicker() => Ticker;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(Commands);
        base.AnnounceHiddenReferences(queue);
    }

    public override bool OnObjectDelete()
    {
        Toolkit.Display.Release(Handle);
        return base.OnObjectDelete();
    }

    public void sizeChanged(int w, int h)
    {
    }
}