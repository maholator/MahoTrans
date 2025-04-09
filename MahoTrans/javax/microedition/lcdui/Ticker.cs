// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Ticker : Object
{
    [String]
    public Reference Text;

    [InitMethod]
    public void Init([String] Reference str)
    {
        base.Init();
        Text = str;
    }

    [return: String]
    public Reference getString() => Text;

    public void setString([String] Reference str)
    {
        Text = str;
        Toolkit.Display.TickerUpdated();
    }
}
