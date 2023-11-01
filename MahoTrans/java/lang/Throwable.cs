using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Throwable : Object
{
    [String] public Reference Message;

    [InitMethod]
    public void Init()
    {
        Message = Reference.Null;
    }

    [InitMethod]
    public void Init([String] Reference message)
    {
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
        return Jvm.AllocateString($"{JavaClass.Name.Replace('/', '.')}: {Jvm.ResolveString(Message)}");
    }
}