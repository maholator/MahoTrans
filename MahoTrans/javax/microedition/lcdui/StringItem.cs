using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class StringItem : Item
{
    [String] public Reference Text;

    [InitMethod]
    public void Init([String] Reference label, [String] Reference text)
    {
        Label = label;
        Text = text;
    }
}