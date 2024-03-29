// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit which encapsulates image and tools to paint on it. This must be inaccessible from JVM! Get it using
///     <see cref="IImageManager.ResolveGraphics" /> when you need it and abandon once done.
/// </summary>
public interface IGraphics
{
    #region Fills

    void FillRect(int x, int y, int w, int h, uint color);

    void FillRoundRect(int x, int y, int w, int h, int aw, int ah, uint color);

    void FillArc(int x, int y, int w, int h, int begin, int length, uint color);

    void FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color);

    void FillPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, uint color);

    #endregion

    #region Lines

    void DrawLine(int x1, int y1, int x2, int y2, uint color);

    void DrawRoundRect(int x, int y, int w, int h, int aw, int ah, uint color);

    void DrawArc(int x, int y, int w, int h, int begin, int length, uint color);

    void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color);

    void DrawPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, uint color);

    #endregion

    void DrawImage(ImageHandle image, int x, int y, GraphicsAnchor an);

    void DrawImage(ImageHandle image, int fromX, int fromY, int toX, int toY, int w, int h,
        SpriteTransform transform, GraphicsAnchor an);

    void DrawImage(ImageHandle image, int x, int y, ImageManipulation manipulation, GraphicsAnchor an);

    void DrawString(ReadOnlySpan<char> text, int x, int y, GraphicsAnchor an, uint color, FontFace face,
        FontStyle style, int size);

    void DrawARGB32(int[] rgbData, bool transparent, int offset, int scanlength, int x, int y, int width, int height,
        ImageManipulation manipulation);

    void DrawARGB16(short[] rgbData, UShortPixelType type, bool transparent, int offset, int scanlength, int x, int y,
        int width, int height, ImageManipulation manipulation);

    GraphicsClip Clip { get; set; }

    void ClipRect(GraphicsClip clip);

    bool DottedStroke { set; }

    /// <summary>
    ///     Brings graphics to initial state.
    /// </summary>
    void Reset();
}
