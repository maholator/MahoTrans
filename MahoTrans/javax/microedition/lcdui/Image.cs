using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkits;
using MahoTrans.Utils;
using IOException = java.io.IOException;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Image : Object
{
    [JavaIgnore] public ImageHandle Handle;

    [return: JavaType(typeof(Image))]
    public static Reference createImage([String] Reference name)
    {
        var blob = Jvm.GetResource(Jvm.ResolveString(name));
        if (blob == null)
            Jvm.Throw<IOException>();

        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateFromFile(blob.ToUnsigned());
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage([JavaType("[B")] Reference buf, int from, int len)
    {
        var blob = Jvm.ResolveArray<sbyte>(buf).ToUnsigned();

        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateFromFile(new Memory<byte>(blob, from, len));
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage(int w, int h)
    {
        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateBuffer(w, h);
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createRGBImage([JavaType("[I")] Reference rgb, int width, int height, bool alpha)
    {
        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateFromRgb(Jvm.ResolveArray<int>(rgb), width, height, alpha);
        return image.This;
    }

    public bool isMutable() => Toolkit.Images.IsMutable(Handle);

    public int getWidth() => Toolkit.Images.GetWidth(Handle);
    public int getHeight() => Toolkit.Images.GetHeight(Handle);

    public void getRGB([JavaType("[I")] Reference rgbData, int offset, int scanlength, int x, int y, int width,
        int height)
    {
        Toolkit.Images.CopyRgb(Jvm.ResolveArray<int>(rgbData), offset, scanlength, x, y, width, height);
    }

    [return: JavaType(typeof(Graphics))]
    public Reference getGraphics()
    {
        if (!isMutable())
            Jvm.Throw<IllegalStateException>();
        var g = Jvm.AllocateObject<Graphics>();
        g.Handle = Toolkit.Images.GetGraphics(Handle);
        return g.This;
    }
}