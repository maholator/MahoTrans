using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Command : Object
{
    [InitMethod]
    public void Init([String] Reference label, int commandType, int priority)
    {
        Init(label, Reference.Null, commandType, priority);
    }

    [InitMethod]
    public void Init([String] Reference shortLabel, [String] Reference longLabel, int commandType, int priority)
    {
    }
}