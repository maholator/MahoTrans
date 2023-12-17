using MahoTrans.Native;
using MahoTrans.Runtime;
using Newtonsoft.Json;
using Object = java.lang.Object;

// ReSharper disable InconsistentNaming

namespace javax.microedition.lcdui;

public class AlertType : Object
{
    [JsonProperty] [JavaType(typeof(AlertType))]
    public static Reference ALARM;

    [JsonProperty] [JavaType(typeof(AlertType))]
    public static Reference CONFIRMATION;

    [JsonProperty] [JavaType(typeof(AlertType))]
    public static Reference ERROR;

    [JsonProperty] [JavaType(typeof(AlertType))]
    public static Reference INFO;

    [JsonProperty] [JavaType(typeof(AlertType))]
    public static Reference WARNING;

    [InitMethod]
    public void Init(int type)
    {
        base.Init();
        Type = (AlertTypeEnum)type;
    }

    [JavaIgnore] public AlertTypeEnum Type;

    [ClassInit]
    public static void ClInit()
    {
        var alarm = Jvm.AllocateObject<AlertType>();
        alarm.Init(1);
        ALARM = alarm.This;

        var confirmation = Jvm.AllocateObject<AlertType>();
        confirmation.Init(2);
        CONFIRMATION = confirmation.This;

        var error = Jvm.AllocateObject<AlertType>();
        error.Init(3);
        ERROR = error.This;

        var info = Jvm.AllocateObject<AlertType>();
        info.Init(4);
        INFO = info.This;

        var warning = Jvm.AllocateObject<AlertType>();
        warning.Init(5);
        WARNING = warning.This;
    }

    public bool playSound([JavaType(typeof(Display))] Reference display)
    {
        //TODO
        return false;
    }
}