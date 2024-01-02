// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.midlet;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class DefaultAlertHandler : Object, CommandListener
{
    public void commandAction([JavaType(typeof(Command))] Reference cmd,
        [JavaType(typeof(Displayable))] Reference displayable)
    {
        if (Jvm.ResolveObject(displayable) is Alert a)
        {
            if (cmd == Alert.DISMISS_COMMAND)
            {
                Jvm.Resolve<Display>(Jvm.Resolve<MIDlet>(Jvm.MidletObject).Display).setCurrent(a.Next);
            }
        }
    }
}