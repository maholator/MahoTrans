// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using com.nokia.mid.ui;
using java.lang;
using MahoTrans.Abstractions;
using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Graphics : Object, DirectGraphics
{
    [JavaIgnore] public GraphicsHandle Handle;

    private IGraphics Implementation => Toolkit.Images.ResolveGraphics(Handle);

    [JavaIgnore] [JsonProperty] private uint _color;
    [JavaIgnore] [JsonProperty] private FontStyle _style;
    [JavaIgnore] [JsonProperty] private FontFace _face;
    [JavaIgnore] [JsonProperty] private int _size;
    [JavaIgnore] [JsonProperty] private int _tx;
    [JavaIgnore] [JsonProperty] private int _ty;

    [JavaType(typeof(Font))] public Reference Font;

    [InitMethod]
    public override void Init()
    {
        base.Init();
        setFont(lcdui.Font.getDefaultFont());
    }

    #region Color / font

    public void setColor(int rgb)
    {
        _color = ((uint)rgb) | 0xFF000000;
    }

    public void setARGBColor(int argbColor) => _color = (uint)argbColor;

    public void setColor(int r, int g, int b)
    {
        var ur = (ulong)r;
        var ug = (ulong)g;
        var ub = (ulong)b;
        _color = (uint)((255UL << 24) | (ur << 16) | (ug << 8) | ub);
    }

    public void setGrayScale(int value)
    {
        if (value < 0 || value > 255)
            Jvm.Throw<IllegalArgumentException>();
        setColor(value, value, value);
    }

    public int getColor() => (int)_color;

    public int getRedComponent() => (int)((_color >> 16) & 0xFF);

    public int getGreenComponent() => (int)((_color >> 8) & 0xFF);

    public int getBlueComponent() => (int)((_color >> 0) & 0xFF);

    public int getGrayScale()
    {
        var r = getRedComponent();
        var g = getGreenComponent();
        var b = getBlueComponent();
        return (r + g + b) / 3;
    }

    public void setFont([JavaType(typeof(Font))] Reference r)
    {
        Font = r;
        var f = Jvm.Resolve<Font>(r);
        _style = f.Style;
        _face = f.Face;
        _size = f.Height;
    }

    [return: JavaType(typeof(Font))]
    public Reference getFont() => Font;

    public void setStrokeStyle(int style)
    {
        //TODO toolkit doesn't support this for now
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
        Implementation.Clip = new GraphicsClip(x + _tx, y + _ty, w, h);
    }

    public void clipRect(int x, int y, int w, int h)
    {
        Implementation.ClipRect(new GraphicsClip(x + _tx, y + _ty, w, h));
    }

    public int getClipX() => Implementation.Clip.X - _tx;
    public int getClipY() => Implementation.Clip.Y - _ty;
    public int getClipWidth() => Implementation.Clip.Width;
    public int getClipHeight() => Implementation.Clip.Height;

    #endregion

    #region Primitives

    public void drawRect(int x, int y, int w, int h)
    {
        drawLine(x, y, x + w, y);
        drawLine(x + w, y, x + w, y + h);
        drawLine(x + w, y + h, x, y + h);
        drawLine(x, y, x, y + h);
    }

    public void drawRoundRect(int x, int y, int w, int h, int arcWidth, int arcHeight)
    {
        Implementation.DrawRoundRect(x + _tx, y + _ty, w, h, arcWidth, arcHeight, _color);
    }

    public void fillRect(int x, int y, int w, int h) =>
        Implementation.FillRect(x + _tx, y + _ty, w, h, _color);

    public void fillRoundRect(int x, int y, int w, int h, int arcWidth, int arcHeight)
    {
        Implementation.FillRoundRect(x + _tx, y + _ty, w, h, arcWidth, arcHeight, _color);
    }

    public void fillTriangle(int x1, int y1, int x2, int y2, int x3, int y3) =>
        Implementation.FillTriangle(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, x3 + _tx, y3 + _ty, _color);

    public void fillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, int argbColor) =>
        Implementation.FillTriangle(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, x3 + _tx, y3 + _ty, (uint)argbColor);

    public void drawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, int argbColor) =>
        Implementation.DrawTriangle(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, x3 + _tx, y3 + _ty, (uint)argbColor);

    public void drawArc(int x, int y, int w, int h, int s, int e) =>
        Implementation.DrawArc(x + _tx, y + _ty, w, h, s, e, _color);

    public void fillArc(int x, int y, int w, int h, int s, int l) =>
        Implementation.FillArc(x + _tx, y + _ty, w, h, s, l, _color);

    public void drawLine(int x1, int y1, int x2, int y2) =>
        Implementation.DrawLine(x1 + _tx, y1 + _ty, x2 + _tx, y2 + _ty, _color);

    #endregion

    #region Text

    public void drawString(string text, int x, int y, GraphicsAnchor a)
    {
        Implementation.DrawString(text, x + _tx, y + _ty, a, _color, _face, _style, _size);
    }

    public void drawSubstring(string text, int offset, int len, int x, int y, GraphicsAnchor a)
    {
        var sub = text.AsSpan(offset, len);
        Implementation.DrawString(sub, x + _tx, y + _ty, a, _color, _face, _style, _size);
    }

    public void drawChar(char c, int x, int y, GraphicsAnchor a)
    {
        Span<char> s = stackalloc char[1];
        s[0] = c;
        Implementation.DrawString(s, x + _tx, y + _ty, a, _color, _face, _style, _size);
    }

    public void drawChars(char[] data, int offset, int length, int x, int y, GraphicsAnchor a)
    {
        var sub = data.AsSpan(offset, length);
        Implementation.DrawString(sub, x + _tx, y + _ty, a, _color, _face, _style, _size);
    }

    #endregion

    public void drawImage(Image image, int x, int y, GraphicsAnchor a)
    {
        Implementation.DrawImage(image.Handle, x + _tx, y + _ty, a);
    }

    public void drawImage(Image image, int x, int y, GraphicsAnchor a, ImageManipulation tr)
    {
        Implementation.DrawImage(image.Handle, x + _tx, y + _ty, tr, a);
    }

    public void drawRegion(Image image, int x_src, int y_src, int width, int height,
        SpriteTransform transform, int x_dest, int y_dest, GraphicsAnchor anchor)
    {
        var t = transform;
        var a = anchor;
        Implementation.DrawImage(image.Handle, x_src, y_src, x_dest + _tx, y_dest + _ty, width, height, t, a);
    }

    public void drawRGB(int[] rgbData, int offset, int scanlength, int x, int y, int width,
        int height, bool processAlpha)
    {
        Implementation.DrawARGB32(rgbData, processAlpha, offset, scanlength, x + _tx, y + _ty, width, height);
    }

    public void fillPolygon(int[] x, int xFrom, int[] y, int yFrom, int count, int argb)
    {
        var xm = x.AsSpan(xFrom, count);
        var ym = y.AsSpan(yFrom, count);
        Implementation.FillPolygon(xm, ym, (uint)argb);
    }

    public void drawPolygon(int[] x, int xFrom, int[] y, int yFrom, int count, int argb)
    {
        var xm = x.AsSpan(xFrom, count);
        var ym = y.AsSpan(yFrom, count);
        Implementation.DrawPolygon(xm, ym, (uint)argb);
    }

    public int getNativePixelFormat() => DirectGraphics.TYPE_INT_8888_ARGB;

    public void drawPixels(short[] pixels, bool transparency, int offset, int scanlength,
        int x, int y, int width, int height, int manipulation, int format)
    {
        UShortPixelType f = format switch
        {
            DirectGraphics.TYPE_USHORT_565_RGB => UShortPixelType.Argb0565,
            DirectGraphics.TYPE_USHORT_1555_ARGB => UShortPixelType.Argb1555,
            DirectGraphics.TYPE_USHORT_4444_ARGB => UShortPixelType.Argb4444,
            _ => throwUShortFormat()
        };

        Implementation.DrawARGB16(pixels, f, transparency, offset, scanlength, x, y, width, height,
            (ImageManipulation)manipulation);
    }

    private static UShortPixelType throwUShortFormat()
    {
        Jvm.Throw<IllegalArgumentException>();
        return default;
    }

    public override bool OnObjectDelete()
    {
        Toolkit.Images.ReleaseGraphics(Handle);
        return false;
    }

    public void Reset()
    {
        setFont(lcdui.Font.getDefaultFont());
        _color = 0;
        _tx = 0;
        _ty = 0;
        Implementation.Reset();
    }
}