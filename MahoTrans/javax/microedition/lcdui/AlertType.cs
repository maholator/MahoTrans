// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

// ReSharper disable InconsistentNaming

namespace javax.microedition.lcdui;

public class AlertType : Object
{
    [InitMethod]
    public void Init(int type)
    {
        base.Init();
        Type = (AlertTypeEnum)type;
    }

    [JavaIgnore]
    public AlertTypeEnum Type;

    [ClassInit]
    public static void ClInit()
    {
        var alarm = Jvm.Allocate<AlertType>();
        alarm.Init(1);
        NativeStatics.AlarmAlertType = alarm.This;

        var confirmation = Jvm.Allocate<AlertType>();
        confirmation.Init(2);
        NativeStatics.ConfirmationAlertType = confirmation.This;

        var error = Jvm.Allocate<AlertType>();
        error.Init(3);
        NativeStatics.ErrorAlertType = error.This;

        var info = Jvm.Allocate<AlertType>();
        info.Init(4);
        NativeStatics.InfoAlertType = info.This;

        var warning = Jvm.Allocate<AlertType>();
        warning.Init(5);
        NativeStatics.WarningAlertType = warning.This;
    }

    public bool playSound([JavaType(typeof(Display))] Reference display)
    {
        //TODO
        return false;
    }
}
