using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Item : Object
{
    [JavaIgnore] public DisplayableHandle Owner;

    [String] public Reference Label;

    [return: String]
    public Reference getLabel() => Label;
}