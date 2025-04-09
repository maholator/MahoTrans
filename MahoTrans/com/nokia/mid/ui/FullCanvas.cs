// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using javax.microedition.lcdui;
using MahoTrans.Native;

namespace com.nokia.mid.ui;

public class FullCanvas : Canvas
{
    [InitMethod]
    public override void Init()
    {
        base.Init();
        setFullScreenMode(true);
    }
}
