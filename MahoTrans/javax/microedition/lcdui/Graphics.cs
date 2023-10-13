using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Graphics : Object
{
    [JavaIgnore] public IGraphics Implementation = null!;

    [JavaIgnore] private uint _color;
    [JavaIgnore] private FontStyle _style;
    [JavaIgnore] private FontFace _face;
    [JavaIgnore] private int _size;
    [JavaIgnore] private int _tx;
    [JavaIgnore] private int _ty;
    [JavaIgnore] private GraphicsClip _clip = new(0, 0, int.MaxValue, int.MaxValue);

    #region Setters

    public void setColor(int argb)
    {
        _color = (uint)argb;
    }

    public void setColor(int r, int g, int b)
    {
        var ur = (ulong)r;
        var ug = (ulong)g;
        var ub = (ulong)b;
        _color = (uint)((255UL << 24) | (ur << 16) | (ug << 8) | ub);
    }

    public void setFont([JavaType(typeof(Font))] Reference r)
    {
        var f = Heap.Resolve<Font>(r);
        _style = f.Style;
        _face = f.Face;
        _size = f.Height;
    }

    #endregion

    #region Translation

    public void translate(int x, int y)
    {
        _tx += x;
        _ty += y;
    }

    public int getTranslateX() => _tx;
    public int getTranslateY() => _ty;

    #endregion

    #region Clipping

    public void setClip(int x, int y, int w, int h)
    {
        //TODO
    }

    public void clipRect(int x, int y, int w, int h)
    {
        //TODO
    }

    public int getClipX() => _clip.X;
    public int getClipY() => _clip.Y;
    public int getClipWidth() => _clip.Width;
    public int getClipHeight() => _clip.Height;

    #endregion

    #region Primitives

    public void drawRect(int x, int y, int w, int h)
    {
        drawLine(x, y, x + w, y);
        drawLine(x + w, y, x + w, y + h);
        drawLine(x + w, y + h, x, y + h);
        drawLine(x, y + h, x, y);
    }

    public void drawRoundRect(int x, int y, int w, int h, int arcWidth, int arcHeight)
    {
        // TODO
    }

    public void fillRect(int x, int y, int w, int h) =>
        Implementation.FillRect(x + _tx, y + _ty, w, h, _color, _clip);

    public void fillRoundRect(int x, int y, int w, int h, int arcWidth, int arcHeight)
    {
        Implementation.FillRoundRect(x + _tx, y + _ty, w, h, arcWidth, arcHeight, _color, _clip);
    }

    public void fillTriangle(int x1, int y1, int x2, int y2, int x3, int y3) =>
        Implementation.FillTriangle(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, x3 + _tx, y3 + _ty, _color, _clip);

    public void drawArc(int x, int y, int w, int h, int s, int e) =>
        Implementation.DrawArc(x + _tx, y + _ty, w, h, s, e, _color, _clip);

    public void fillArc(int x, int y, int w, int h, int s, int l) =>
        Implementation.FillArc(x + _tx, y + _ty, w, h, s, l, _color, _clip);

    public void drawLine(int x1, int y1, int x2, int y2) =>
        Implementation.DrawLine(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, _color, _clip);

    #endregion

    public void drawString([String] Reference str, int x, int y, int a)
    {
        var text = Heap.ResolveString(str);
        Implementation.DrawString(text, x + _tx, y + _ty, (GraphicsAnchor)a, _color, _face, _style, _size, _clip);
    }

    public void drawImage([JavaType(typeof(Image))] Reference image, int x, int y, int a)
    {
        var res = Heap.Resolve<Image>(image);
        Implementation.DrawImage(res.Handle, x + _tx, y + _ty, (GraphicsAnchor)a, _clip);
    }

    public void drawRegion([JavaType(typeof(Image))] Reference image, int x_src, int y_src, int width, int height,
        int transform, int x_dest,
        int y_dest, int anchor)
    {
        var res = Heap.Resolve<Image>(image);
        Implementation.DrawImage(res.Handle, x_src, y_src, x_dest + _tx, y_dest + _ty, width, height,
            (SpriteTransform)transform, (GraphicsAnchor)anchor, _clip);
    }
}