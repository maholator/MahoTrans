namespace MahoTrans.Toolkit;

public interface IImageManager
{
    ImageDescriptor CreateBuffer(int width, int height);

    ImageDescriptor CreateFromFile(Memory<byte> file);

    ImageDescriptor CreateFromRgb(int[] rgb, int w, int h, bool alpha);

    ImageDescriptor CreateCopy(ImageDescriptor image);

    ImageDescriptor CreateCopy(ImageDescriptor image, int x, int y, int w, int h, int tr);

    int GetWidth(ImageDescriptor image);

    int GetHeight(ImageDescriptor image);

    bool IsMutable(ImageDescriptor image);

    void CopyRgb(int[] target, int offset, int scanlength, int x, int y, int w, int h);

    void ReleaseImage(ImageDescriptor image);

    GraphicsDescriptor GetGraphics(ImageDescriptor image);

    IGraphics ResolveGraphics(GraphicsDescriptor descriptor);

    void ReleaseGraphics(GraphicsDescriptor descriptor);
}