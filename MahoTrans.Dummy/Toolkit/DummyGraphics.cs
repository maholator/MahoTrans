using javax.microedition.lcdui;
using MahoTrans.Toolkit;

namespace MahoTrans.Dummy.Toolkit;

public class DummyGraphics : IGraphics
{
    public void FillRect(int x, int y, int w, int h, uint color, GraphicsClip clip)
    {
    }

    public void FillRoundRect(int x, int y, int w, int h, int aw, int ah, uint color, GraphicsClip clip)
    {
    }

    public void FillArc(int x, int y, int w, int h, int begin, int end, uint color, GraphicsClip clip)
    {
    }

    public void DrawLine(int x1, int y1, int x2, int y2, uint color, GraphicsClip clip)
    {
    }

    public void FillTriangle(int x1, int y1, int x2, int y2, int x3, int y3, uint color, GraphicsClip clip)
    {
    }

    public void DrawImage(IImage image, int x, int y, GraphicsAnchor an, GraphicsClip clip)
    {
    }

    public void DrawImage(IImage image, int fromX, int fromY, int toX, int toY, int w, int h, SpriteTransform transform,
        GraphicsAnchor an, GraphicsClip clip)
    {
    }

    public void DrawString(string text, int x, int y, GraphicsAnchor an, uint color, FontFace face, FontStyle style,
        int size,
        GraphicsClip clip)
    {
    }

    public void DrawArc(int x, int y, int w, int h, int begin, int length, uint color, GraphicsClip clip)
    {
    }
}