// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui.game;

public class Layer : Object
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public bool Visible;

    [InitMethod]
    public void Init(int w, int h)
    {
        Visible = true;
        if (w < 0 || h < 0)
            Jvm.Throw<IllegalArgumentException>();
        Width = w;
        Height = h;
    }

    public void setPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void move(int x, int y)
    {
        X += x;
        Y += y;
    }

    public void setVisible(bool visible)
    {
        Visible = visible;
    }

    public int getX() => X;

    public int getY() => Y;

    public int getWidth() => Width;

    public int getHeight() => Height;

    public bool isVisible() => Visible;

    public void paint(Reference g)
    {
        throw new AbstractCall();
    }
}
