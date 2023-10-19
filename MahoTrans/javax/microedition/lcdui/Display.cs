using java.lang;
using javax.microedition.ams.events;
using javax.microedition.midlet;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Display : Object
{
    [JavaType(typeof(Displayable))] public Reference Current;

    [return: JavaType(typeof(Display))]
    public static Reference getDisplay([JavaType(typeof(MIDlet))] Reference midletRef)
    {
        var midlet = Jvm.Resolve<MIDlet>(midletRef);
        if (midlet.Display.IsNull)
        {
            var disp = Jvm.AllocateObject<Display>();
            midlet.Display = disp.This;
        }

        return midlet.Display;
    }

    [return: JavaType(typeof(Displayable))]
    public Reference getCurrent()
    {
        return Current;
    }

    public void setCurrent([JavaType(typeof(Displayable))] Reference d)
    {
        Current = d;
        if (d.IsNull)
            Toolkit.Display.SetNullCurrent();
        else
            Toolkit.Display.SetCurrent(Jvm.Resolve<Displayable>(d).Handle);
    }

    public void callSerially([JavaType(typeof(Runnable))] Reference r)
    {
        Jvm.EventQueue.Enqueue<ActionEvent>(x => x.Target = r);
    }

    public bool vibrate(int dur)
    {
        //TODO
        return true;
    }
}