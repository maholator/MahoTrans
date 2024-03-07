// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class Alert : Screen
{
    public const int FOREVER = -2;

    [JavaType(typeof(Displayable))]
    public Reference Next;

    [ClassInit]
    public static void ClInit()
    {
        var dismiss = Jvm.Allocate<Command>();
        dismiss.Init(Jvm.AllocateString(""), Command.OK, 0);
        NativeStatics.AlertDismissCommand = dismiss.This;
    }

    [InitMethod]
    public void Init([String] Reference title)
    {
        Init(title, Reference.Null, Reference.Null, Reference.Null);
    }

    [InitMethod]
    public void Init([String] Reference title, [String] Reference alertText,
        [JavaType(typeof(Image))] Reference alertImage, [JavaType(typeof(AlertType))] Reference alertType)
    {
        base.Init();
        setTitle(title);
        Text = alertText;
        Image = alertImage;
        Type = alertType;
        Timeout = getDefaultTimeout();
        Commands.Add(NativeStatics.AlertDismissCommand);
        setCommandListener(Jvm.Allocate<DefaultAlertHandler>().This);
        // invalidate is necessary to notify toolkit about non-null implicit command
        Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
    }

    [String]
    public Reference Text;

    [JavaType(typeof(Image))]
    public Reference Image;

    [JavaType(typeof(AlertType))]
    public Reference Type;

    public int Timeout;

    public int getDefaultTimeout() => FOREVER;

    public int getTimeout() => Timeout;

    public void setTimeout(int timeout)
    {
        if (timeout <= 0 && timeout != FOREVER)
            Jvm.Throw<IllegalArgumentException>();

        Timeout = timeout;
    }

    [return: JavaType(typeof(AlertType))]
    public Reference getType() => Type;

    public void setType([JavaType(typeof(AlertType))] Reference type)
    {
        Type = type;
        Toolkit.Display.ContentUpdated(Handle);
    }

    [return: String]
    public Reference getString() => Text;

    public void setString([String] Reference text)
    {
        Text = text;
        Toolkit.Display.ContentUpdated(Handle);
    }

    [return: JavaType(typeof(Image))]
    public Reference getImage() => Image;

    public void setImage([JavaType(typeof(Image))] Reference image)
    {
        Image = image;
        Toolkit.Display.ContentUpdated(Handle);
    }

    //TODO set/get Indicator

    public new void addCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd == NativeStatics.AlertDismissCommand)
            return;
        base.addCommand(cmd);
        if (Commands.Contains(NativeStatics.AlertDismissCommand) && Commands.Count != 1)
        {
            Commands.Remove(NativeStatics.AlertDismissCommand);
            Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
        }
    }

    public new void removeCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd == NativeStatics.AlertDismissCommand)
            return;
        base.removeCommand(cmd);
        if (Commands.Count == 0)
        {
            Commands.Add(NativeStatics.AlertDismissCommand);
            Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
        }
    }
}
