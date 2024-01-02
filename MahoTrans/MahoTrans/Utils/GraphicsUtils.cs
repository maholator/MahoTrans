// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using javax.microedition.lcdui;
using Object = java.lang.Object;

namespace MahoTrans.Utils;

public static class GraphicsUtils
{
    public static (int, int) Anchor(this GraphicsAnchor anchor, int x, int y, int w, int h)
    {
        if (anchor == 0)
            return (x, y);

        var ah = anchor & GraphicsAnchor.AllHorizontal;
        var av = anchor & GraphicsAnchor.AllVertical;

        switch (ah)
        {
            case GraphicsAnchor.Left:
                break;

            case GraphicsAnchor.HCenter:
                x -= w / 2;
                break;

            case GraphicsAnchor.Right:
                x -= w;
                break;

            default:
                Object.Jvm.Throw<IllegalArgumentException>();
                break;
        }

        switch (av)
        {
            case GraphicsAnchor.Top:
                break;

            case GraphicsAnchor.VCenter:
                y -= h / 2;
                break;

            case GraphicsAnchor.Bottom:
                y -= h;
                break;

            default:
                Object.Jvm.Throw<IllegalArgumentException>();
                break;
        }

        return (x, y);
    }
}