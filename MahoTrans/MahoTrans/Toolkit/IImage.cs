namespace MahoTrans.Toolkit;

public interface IImage
{
    IGraphics GetGraphics();
    bool IsMutable { get; }
    int Width { get; }
    int Height { get; }
    void GetRGB(int[] rgbData, int offset, int scanlength, int x, int y, int width, int height);
}