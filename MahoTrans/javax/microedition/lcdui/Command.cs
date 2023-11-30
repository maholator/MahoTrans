using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

// ReSharper disable InconsistentNaming

namespace javax.microedition.lcdui;

public class Command : Object
{
    public static int BACK = 2;

    public static int CANCEL = 3;

    public static int EXIT = 7;

    public static int HELP = 5;

    public static int ITEM = 8;

    public static int OK = 4;

    public static int SCREEN = 1;

    public static int STOP = 6;

    [InitMethod]
    public void Init([String] Reference label, int commandType, int priority)
    {
        Init(label, Reference.Null, commandType, priority);
    }

    [InitMethod]
    public void Init([String] Reference shortLabel, [String] Reference longLabel, int commandType, int priority)
    {
        if (shortLabel.IsNull)
            Jvm.Throw<NullPointerException>();

        if (commandType < SCREEN || commandType > ITEM)
            Jvm.Throw<IllegalArgumentException>();

        CommandType = commandType;
        Label = shortLabel;
        LongLabel = longLabel;
        Priority = priority;
    }

    public int CommandType;
    public Reference Label;
    public Reference LongLabel;
    public int Priority;

    [return: String]
    public Reference getLabel() => Label;

    [return: String]
    public Reference getLongLabel() => LongLabel;

    public int getCommandType() => CommandType;

    public int getPriority() => Priority;
}