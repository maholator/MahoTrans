using MahoTrans.Toolkit;

namespace MahoTrans.Dummy;

public class DummyImage : IImage
{
    public IGraphics GetGraphics()
    {
        throw new NotImplementedException();
    }

    public bool IsMutable { get; } = true;
    public int Width { get; } = 100;
    public int Height { get; } = 100;
    public void GetRGB(int[] rgbData, int offset, int scanlength, int x, int y, int width, int height)
    {
    }
}