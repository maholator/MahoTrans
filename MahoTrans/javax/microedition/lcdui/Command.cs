// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

// ReSharper disable InconsistentNaming

namespace javax.microedition.lcdui;

public class Command : Object
{
    public const int BACK = 2;
    public const int CANCEL = 3;
    public const int EXIT = 7;
    public const int HELP = 5;
    public const int ITEM = 8;
    public const int OK = 4;
    public const int SCREEN = 1;
    public const int STOP = 6;

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
