using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

/// <summary>
/// Toolkit which encapsulates image and tools to paint on it. This must be inaccessible from JVM! Get it using <see cref="IImageManager.ResolveGraphics"/> when you need it and abandon once done.
/// </summary>
public interface IGraphics
{
    void FillRect(int x, int y, int w, int h, uint color, GraphicsClip clip);

    void FillRoundRect(int x, int y, int w, int h, int aw, int ah, uint color, GraphicsClip clip);

    void FillArc(int x, int y, int w, int h, int begin, int length, uint color, GraphicsClip clip);

    void DrawLine(int x1, int y1, int x2, int y2, uint color, GraphicsClip clip);

    void FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color, GraphicsClip clip);
    void DrawImage(ImageDescriptor image, int x, int y, GraphicsAnchor an, GraphicsClip clip);

    void DrawImage(ImageDescriptor image, int fromX, int fromY, int toX, int toY, int w, int h,
        SpriteTransform transform,
        GraphicsAnchor an, GraphicsClip clip);

    void DrawImage(ImageDescriptor image, int x, int y, ImageManipulation manipul, GraphicsAnchor an,
        GraphicsClip clip);

    void DrawString(string text, int x, int y, GraphicsAnchor an, uint color, FontFace face, FontStyle style, int size,
        GraphicsClip clip);

    void DrawArc(int x, int y, int w, int h, int begin, int length, uint color, GraphicsClip clip);

    void DrawRGB(int[] rgbData, int offset, int scanlength, int x, int y, int width, int height, bool processAlpha,
        GraphicsClip clip);
}