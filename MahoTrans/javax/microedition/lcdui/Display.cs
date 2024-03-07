// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using javax.microedition.ams.events;
using javax.microedition.midlet;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Display : Object
{
    [JavaType(typeof(Displayable))]
    public Reference Current;

    [return: JavaType(typeof(Display))]
    public static Reference getDisplay([JavaType(typeof(MIDlet))] Reference midletRef)
    {
        var midlet = Jvm.Resolve<MIDlet>(midletRef);
        if (midlet.Display.IsNull)
        {
            var disp = Jvm.Allocate<Display>();
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
        if (d.IsNull)
        {
            Toolkit.Display.SetNullCurrent();
            return;
        }

        // we ignore double-sets of displayables, i guess?
        //TODO notify toolkit about "screen resume"
        if (Current == d)
            return;

        if (Jvm.ResolveObject(d) is Alert a)
            a.Next = Current;

        Current = d;
        Toolkit.Display.SetCurrent(Jvm.Resolve<Displayable>(d).Handle);
    }

    public void setCurrent([JavaType(typeof(Alert))] Reference alert, [JavaType(typeof(Displayable))] Reference next)
    {
        if (next.IsNull || alert.IsNull)
            Jvm.Throw<NullPointerException>();

        // i added check above for readability
        var a = Jvm.Resolve<Alert>(alert);
        a.Next = next;

        Current = alert;
        Toolkit.Display.SetCurrent(a.Handle);
    }

    public void setCurrentItem([JavaType(typeof(Item))] Reference item)
    {
        var i = Jvm.Resolve<Item>(item);
        if (i.Owner == default)
            Jvm.Throw<IllegalStateException>();

        Toolkit.Display.FocusItem(i.Owner, item);
    }

    public void callSerially([JavaType(typeof(Runnable))] Reference r)
    {
        Jvm.EventQueue.Enqueue<ActionEvent>(x => x.Target = r);
    }

    public bool isColor() => true;

    public int numAlphaLevels() => 256;

    public int numColors() => 256 * 256 * 256;

    public bool vibrate(int dur) => true;

    public bool flashBacklight(int duration) => false;

    public int getBorderStyle(bool highlighted) => 0; // solid border

    public int getColor(int colorSpecifier)
    {
        switch (colorSpecifier)
        {
            case COLOR_BACKGROUND:
                return unchecked((int)0xFF000000);
            case COLOR_FOREGROUND:
                return unchecked((int)0xFFFFFFFF);
            case COLOR_HIGHLIGHTED_BACKGROUND:
                return unchecked((int)0xFF4791DC);
            case COLOR_HIGHLIGHTED_FOREGROUND:
                return unchecked((int)0xFFFFFFFF);
            case COLOR_BORDER:
                return unchecked((int)0xFFFFFFFF);
            case COLOR_HIGHLIGHTED_BORDER:
                return unchecked((int)0xFFFFFFFF);
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }
    }

    public const int ALERT = 3;
    public const int CHOICE_GROUP_ELEMENT = 2;
    public const int COLOR_BACKGROUND = 0;
    public const int COLOR_BORDER = 4;
    public const int COLOR_FOREGROUND = 1;
    public const int COLOR_HIGHLIGHTED_BACKGROUND = 2;
    public const int COLOR_HIGHLIGHTED_BORDER = 5;
    public const int COLOR_HIGHLIGHTED_FOREGROUND = 3;
    public const int LIST_ELEMENT = 1;
}
