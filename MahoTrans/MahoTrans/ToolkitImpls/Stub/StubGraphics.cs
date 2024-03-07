// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using MahoTrans.Abstractions;
using MahoTrans.Handles;

namespace MahoTrans.ToolkitImpls.Stub;

/// <summary>
///     Graphics implementation that does nothing. Clipping operations are handled correctly.
/// </summary>
public class StubGraphics : IGraphics
{
    public void FillRect(int x, int y, int w, int h, uint color)
    {
    }

    public void FillRoundRect(int x, int y, int w, int h, int aw, int ah, uint color)
    {
    }

    public void FillArc(int x, int y, int w, int h, int begin, int length, uint color)
    {
    }

    public void FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color)
    {
    }

    public void FillPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, uint color)
    {
    }

    public void DrawLine(int x1, int y1, int x2, int y2, uint color)
    {
    }

    public void DrawRoundRect(int x, int y, int w, int h, int aw, int ah, uint color)
    {
    }

    public void DrawArc(int x, int y, int w, int h, int begin, int length, uint color)
    {
    }

    public void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color)
    {
    }

    public void DrawPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, uint color)
    {
    }

    public void DrawImage(ImageHandle image, int x, int y, GraphicsAnchor an)
    {
    }

    public void DrawImage(ImageHandle image, int fromX, int fromY, int toX, int toY, int w, int h,
        SpriteTransform transform,
        GraphicsAnchor an)
    {
    }

    public void DrawImage(ImageHandle image, int x, int y, ImageManipulation manipulation, GraphicsAnchor an)
    {
    }

    public void DrawString(ReadOnlySpan<char> text, int x, int y, GraphicsAnchor an, uint color, FontFace face,
        FontStyle style,
        int size)
    {
    }

    public void DrawARGB32(int[] rgbData, bool transparent, int offset, int scanlength, int x, int y, int width,
        int height)
    {
    }

    public void DrawARGB32(int[] rgbData, bool transparent, int offset, int scanlength, int x, int y, int width,
        int height,
        ImageManipulation manipulation)
    {
    }

    public void DrawARGB16(short[] rgbData, UShortPixelType type, bool transparent, int offset, int scanlength, int x,
        int y,
        int width, int height, ImageManipulation manipulation)
    {
    }

    public void ClipRect(GraphicsClip clip)
    {
        Rectangle r = Rectangle.Intersect(new Rectangle(Clip.X, Clip.Y, Clip.Width, Clip.Height),
            new Rectangle(clip.X, clip.Y, clip.Width, clip.Height));
        Clip = new GraphicsClip(r.X, r.Y, r.Width, r.Height);
    }

    public void Reset() => Clip = new GraphicsClip(0, 0, 9999, 9999);

    public GraphicsClip Clip { get; set; }

    public bool DottedStroke
    {
        set { }
    }
}
