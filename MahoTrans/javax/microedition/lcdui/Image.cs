// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
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
        var blob = Jvm.GetResource(Jvm.ResolveString(name), null);
        if (blob == null)
            Jvm.Throw<IOException>();

        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateFromFile(blob.ToUnsigned());
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage([JavaType("[B")] Reference buf, int from, int len)
    {
        try
        {
            var blob = Jvm.ResolveArray<sbyte>(buf).ToUnsigned();

            var image = Jvm.AllocateObject<Image>();
            image.Handle = Toolkit.Images.CreateFromFile(new Memory<byte>(blob, from, len));
            return image.This;
        }
        catch
        {
            Jvm.Throw<IOException>();
        }
        return default;
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

    [return: JavaType(typeof(Image))]
    public static Reference createImage___copy([JavaType(typeof(Image))] Reference source)
    {
        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateCopy(Jvm.Resolve<Image>(source).Handle);
        return image.This;
    }

    [JavaDescriptor("(Ljava/io/InputStream;)Ljavax/microedition/lcdui/Image;")]
    public static JavaMethodBody createImage___stream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);

        // buf = new byte[4096];
        b.AppendConstant(4096);
        b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
        b.Append(JavaOpcode.astore_1);

        // count = 0
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_2);

        // readBuf = new byte[4096];
        b.AppendConstant(4096);
        b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
        b.Append(JavaOpcode.astore_3);

        using (var loop = b.BeginLoop(JavaOpcode.ifle))
        {
            // read = in.read()
            b.Append(JavaOpcode.aload_0);
            b.Append(JavaOpcode.aload_3);
            b.AppendVirtcall("read", "([B)I");
            b.Append(JavaOpcode.istore, 4);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iadd);
            b.Append(JavaOpcode.aload, 1);
            b.Append(JavaOpcode.arraylength);

            // grow buffer
            using (b.AppendGoto(JavaOpcode.if_icmple)) // if(count + readLen > buf.length)
            {
                // tmp = new byte[count * 2];
                b.Append(JavaOpcode.iload_2);
                b.Append(JavaOpcode.iconst_2);
                b.Append(JavaOpcode.imul);
                b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
                b.Append(JavaOpcode.astore, 5);

                // System.arraycopy(tmp, 0, buf, count, read);
                b.Append(JavaOpcode.aload_1);
                b.Append(JavaOpcode.iconst_0);
                b.Append(JavaOpcode.aload, 5);
                b.Append(JavaOpcode.iconst_0);
                b.Append(JavaOpcode.iload_2);
                b.AppendStaticCall(new NameDescriptorClass("arraycopy", "(Ljava/lang/Object;ILjava/lang/Object;II)V",
                    typeof(java.lang.System)));

                // buf = tmp
                b.Append(JavaOpcode.aload, 5);
                b.Append(JavaOpcode.astore_1);
            }

            // System.arraycopy(readBuf, 0, buf, count, read);
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.iconst_0);
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.AppendStaticCall(new NameDescriptorClass("arraycopy", "(Ljava/lang/Object;ILjava/lang/Object;II)V",
                typeof(java.lang.System)));

            // count += read
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iadd);
            b.Append(JavaOpcode.istore_2);

            b.Append(JavaOpcode.iload, 4);
        }

        // return Image.createImage(buf, 0, count);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.iload_2);
        b.AppendStaticCall(new NameDescriptorClass("createImage", "([BII)Ljavax/microedition/lcdui/Image;",
            typeof(Image)));
        b.AppendReturnReference();

        return b.Build(5, 6);
    }


    public bool isMutable() => Toolkit.Images.IsMutable(Handle);

    public int getWidth() => Toolkit.Images.GetWidth(Handle);
    public int getHeight() => Toolkit.Images.GetHeight(Handle);

    public void getRGB([JavaType("[I")] Reference rgbData, int offset, int scanlength, int x, int y, int width,
        int height)
    {
        Toolkit.Images.CopyRgb(Handle, Jvm.ResolveArray<int>(rgbData), offset, scanlength, x, y, width, height);
    }

    [return: JavaType(typeof(Graphics))]
    public Reference getGraphics()
    {
        if (!isMutable())
            Jvm.Throw<IllegalStateException>();
        var g = Jvm.AllocateObject<Graphics>();
        g.Init();
        g.Handle = Toolkit.Images.GetGraphics(Handle);
        return g.This;
    }

    public override bool OnObjectDelete()
    {
        Toolkit.Images.ReleaseImage(Handle);
        return false;
    }
}