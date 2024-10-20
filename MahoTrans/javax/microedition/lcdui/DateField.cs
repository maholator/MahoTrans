// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using TimeZone = java.util.TimeZone;

namespace javax.microedition.lcdui;

public class DateField : Item
{
    public int InputMode;

    public Reference Date;

    [InitMethod]
    public void Init([String] Reference label, int mode) => Init(label, mode, Reference.Null);

    [InitMethod]
    public void Init([String] Reference label, int mode, [JavaType(typeof(TimeZone))] Reference timeZone)
    {
        base.Init();
        if (mode < DATE || mode > DATE_TIME)
            Jvm.Throw<IllegalArgumentException>();
        Label = label;
        InputMode = mode;
        Date = Reference.Null;
        //TODO timezone should be used... I guess? But how?
    }

    [return: JavaType(typeof(Date))]
    public Reference getDate() => Date;

    public void setDate([JavaType(typeof(Date))] Reference date)
    {
        Date = date;
        NotifyToolkit();
    }

    public int getInputMode() => InputMode;

    public void setInputMode(int mode)
    {
        InputMode = mode;
        NotifyToolkit();
    }

    public const int DATE = 1;
    public const int TIME = 2;
    public const int DATE_TIME = 3;
}
