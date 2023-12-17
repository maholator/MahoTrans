// ReSharper disable InconsistentNaming

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Newtonsoft.Json;

namespace javax.microedition.lcdui;

public class Alert : Screen
{
    public static int FOREVER = -2;

    [JsonProperty] [JavaType(typeof(Command))]
    public static Reference DISMISS_COMMAND;

    [ClassInit]
    public static void ClInit()
    {
        var dismiss = Jvm.AllocateObject<Command>();
        dismiss.Init(Jvm.AllocateString(""), Command.OK, 0);
        DISMISS_COMMAND = dismiss.This;
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
        Commands.Add(DISMISS_COMMAND);
        // invalidate is necessary to notify toolkit about non-null implicit command
        Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
    }

    [String] public Reference Text;

    [JavaType(typeof(Image))] public Reference Image;

    [JavaType(typeof(AlertType))] public Reference Type;

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
        if (cmd == DISMISS_COMMAND)
            return;
        base.addCommand(cmd);
        if (Commands.Contains(DISMISS_COMMAND) && Commands.Count != 1)
        {
            Commands.Remove(DISMISS_COMMAND);
            Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
        }
    }

    public new void removeCommand([JavaType(typeof(Command))] Reference cmd)
    {
        if (cmd == DISMISS_COMMAND)
            return;
        base.removeCommand(cmd);
        if (Commands.Count == 0)
        {
            Commands.Add(DISMISS_COMMAND);
            Toolkit.Display.CommandsUpdated(Handle, Commands, Reference.Null);
        }
    }
}