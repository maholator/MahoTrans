namespace javax.microedition.lcdui;

public struct GraphicsClip
{
    public int X, Y, Width, Height;

    public GraphicsClip(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}