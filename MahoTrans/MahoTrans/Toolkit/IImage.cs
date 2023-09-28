namespace MahoTrans.Toolkit;

public interface IImage
{
    IGraphics GetGraphics();
    bool IsMutable { get; }
    int Width { get; }
    int Height { get; }
}