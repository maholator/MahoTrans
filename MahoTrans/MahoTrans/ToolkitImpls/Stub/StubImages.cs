// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Handles;

namespace MahoTrans.ToolkitImpls.Stub;

public class StubImages : IImageManager
{
    private readonly List<int> _aliveImages = new();

    private int NextId => _aliveImages.Count != 0 ? _aliveImages.Max() + 1 : 1;

    private ImageHandle create()
    {
        var id = NextId;
        _aliveImages.Add(id);
        return new ImageHandle(id);
    }

    public ImageHandle CreateBuffer(int width, int height) => create();

    public ImageHandle CreateBuffer(int width, int height, int argb) => create();

    public ImageHandle CreateBufferFromFile(ReadOnlySpan<byte> file) => create();

    public ImageHandle CreateFromFile(ReadOnlySpan<byte> file) => create();

    public ImageHandle CreateFromRgb(int[] rgb, int w, int h, bool alpha) => create();

    public ImageHandle CreateCopy(ImageHandle image) => create();

    public ImageHandle CreateCopy(ImageHandle image, int x, int y, int w, int h, SpriteTransform tr) => create();

    public int GetWidth(ImageHandle image) => 128;

    public int GetHeight(ImageHandle image) => 128;

    public bool IsMutable(ImageHandle image) => true;

    public void CopyRgb(ImageHandle image, int[] target, int offset, int scanlength, int x, int y, int w, int h)
    {
    }

    public void ReleaseImage(ImageHandle image)
    {
        _aliveImages.Remove(image);
    }

    public GraphicsHandle GetGraphics(ImageHandle image)
    {
        return new GraphicsHandle(292);
    }

    public IGraphics ResolveGraphics(GraphicsHandle handle)
    {
        return new StubGraphics();
    }

    public void ReleaseGraphics(GraphicsHandle handle)
    {
    }
}
