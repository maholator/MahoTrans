namespace MahoTrans.Toolkits;

public interface IImageManager
{
    ImageHandle CreateBuffer(int width, int height);

    ImageHandle CreateFromFile(Memory<byte> file);

    ImageHandle CreateFromRgb(int[] rgb, int w, int h, bool alpha);

    ImageHandle CreateCopy(ImageHandle image);

    ImageHandle CreateCopy(ImageHandle image, int x, int y, int w, int h, int tr);

    int GetWidth(ImageHandle image);

    int GetHeight(ImageHandle image);

    bool IsMutable(ImageHandle image);

    void CopyRgb(int[] target, int offset, int scanlength, int x, int y, int w, int h);

    void ReleaseImage(ImageHandle image);

    GraphicsHandle GetGraphics(ImageHandle image);

    IGraphics ResolveGraphics(GraphicsHandle handle);

    void ReleaseGraphics(GraphicsHandle handle);
}