// ReSharper disable InconsistentNaming

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class Alert : Screen
{
    public static int FOREVER = -2;

    [JavaType(typeof(Command))] public static Reference DISMISS_COMMAND;

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
        setTitle(title);
        Text = alertText;
        Image = alertImage;
        Type = alertType;
        Timeout = getDefaultTimeout();
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
    }

    [return: String]
    public Reference getString() => Text;

    public void setString([String] Reference text) => Text = text;

    [return: JavaType(typeof(Image))]
    public Reference getImage() => Image;

    public void setImage([JavaType(typeof(Image))] Reference image) => Image = image;

    //TODO set/get Indicator

    //TODO commands
}