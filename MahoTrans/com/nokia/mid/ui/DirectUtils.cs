// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace com.nokia.mid.ui;

public class DirectUtils : Object
{
    [return: JavaType(typeof(DirectGraphics))]
    public static Reference getDirectGraphics([JavaType(typeof(Graphics))] Reference g) => g;

    [return: JavaType(typeof(Image))]
    public static Reference createImage(int width, int height, int color)
    {
        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateBuffer(width, height, color);
        return image.This;
    }

    [return: JavaType(typeof(Image))]
    public static Reference createImage([JavaType("[B")] Reference data, int from, int len)
    {
        var blob = Jvm.ResolveArray<sbyte>(data).ToUnsigned();

        var image = Jvm.AllocateObject<Image>();
        image.Handle = Toolkit.Images.CreateBufferFromFile(new Memory<byte>(blob, from, len));
        return image.This;
    }
}