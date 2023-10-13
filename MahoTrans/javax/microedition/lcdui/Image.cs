using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkit;
using MahoTrans.Utils;
using IOException = java.io.IOException;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Image : Object
{
    [JavaIgnore] public IImage Handle = null!;

    [return: JavaType(typeof(Image))]
    public static Reference createImage([String] Reference name)
    {
        var blob = Heap.State.GetResource(Heap.ResolveString(name));
        if (blob == null)
            Heap.Throw<IOException>();

        var image = Heap.AllocateObject<Image>();
        image.Handle = Heap.State.Toolkit.CreateImmutableImage(blob.ToUnsigned());
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage([JavaType("[B")] Reference buf, int from, int len)
    {
        var blob = Heap.ResolveArray<sbyte>(buf).ToUnsigned().Skip(from).Take(len).ToArray();

        var image = Heap.AllocateObject<Image>();
        image.Handle = Heap.State.Toolkit.CreateImmutableImage(blob);
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage(int w, int h)
    {
        var image = Heap.AllocateObject<Image>();
        image.Handle = Heap.State.Toolkit.CreateMutableImage(w, h);
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createRGBImage([JavaType("[I")] Reference rgb, int width, int height, bool alpha)
    {
        var image = Heap.AllocateObject<Image>();
        image.Handle = Heap.State.Toolkit.CreateImmutableImage(Heap.ResolveArray<int>(rgb), width, height, alpha);
        return image.This;
    }

    public bool isMutable() => Handle.IsMutable;

    public int getWidth() => Handle.Width;
    public int getHeight() => Handle.Height;
}