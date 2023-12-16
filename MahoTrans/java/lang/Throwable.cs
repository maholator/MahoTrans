using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Throwable : Object
{
    [String] public Reference Message;

    [InitMethod]
    public new void Init()
    {
        base.Init();
        Message = Reference.Null;
    }

    [InitMethod]
    public void Init([String] Reference message)
    {
        base.Init();
        Message = message;
    }

    public void printStackTrace()
    {
        Jvm.Toolkit.System.PrintException(this);
    }

    [return: String]
    public Reference getMessage() => Message;

    [return: String]
    public Reference toString()
    {
        if (Message == Reference.Null)
            return Jvm.AllocateString(JavaClass.Name.Replace('/', '.'));
        var msg = $"{JavaClass.Name.Replace('/', '.')}: {Jvm.ResolveStringOrDefault(Message)}";
        return Jvm.AllocateString(msg);
    }
}