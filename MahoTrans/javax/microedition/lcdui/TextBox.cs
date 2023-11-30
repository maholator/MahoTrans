using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class TextBox : Screen
{
    [InitMethod]
    public void Init([String] Reference title, [String] Reference text, int maxSize, int constraints)
    {
        base.Init();
    }
}