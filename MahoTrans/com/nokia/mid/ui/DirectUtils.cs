// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
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
        image.Handle = Toolkit.Images.CreateBufferFromFile(new ReadOnlySpan<byte>(blob, from, len));
        return image.This;
    }

    [return: JavaType(typeof(Font))]
    public static Reference getFont(int face, int style, int height)
    {
        //TODO checks
        var font = Jvm.AllocateObject<Font>();
        font.Face = (FontFace)face;
        font.Style = (FontStyle)style;
        font.Size = default;
        font.Height = height;
        return font.This;
    }
}