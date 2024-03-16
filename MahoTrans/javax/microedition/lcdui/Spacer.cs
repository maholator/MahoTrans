// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class Spacer : Item
{
    public int MinW, MinH;

    [InitMethod]
    public void Init(int w, int h)
    {
        MinW = w;
        MinH = h;
    }

    public new int getMinimumWidth() => MinW;

    public new int getMinimumHeight() => MinH;

    public void setMinimumSize(int minWidth, int minHeight)
    {
        if (minWidth < 0)
            Jvm.Throw<IllegalArgumentException>();
        if (minHeight < 0)
            Jvm.Throw<IllegalArgumentException>();
        MinW = minWidth;
        MinH = minHeight;
        NotifyToolkit();
    }

    public new void setLabel([String] Reference label) => Jvm.Throw<IllegalStateException>();

    public void addCommand(Command cmd) => Jvm.Throw<IllegalStateException>();

    public void setDefaultCommand(Command cmd) => Jvm.Throw<IllegalStateException>();
}
