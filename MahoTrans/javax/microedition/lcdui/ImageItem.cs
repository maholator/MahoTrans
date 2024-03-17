// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ImageItem : Item
{
    [JavaType(typeof(Image))]
    public Reference Image;

    [String]
    public Reference AltText;

    public int Appearance;

    [InitMethod]
    public void Init([String] Reference label, [JavaType(typeof(Image))] Reference image, int layout,
        [String] Reference altText)
    {
        Init(label, image, layout, altText, PLAIN);
    }

    [InitMethod]
    public void Init([String] Reference label, [JavaType(typeof(Image))] Reference image, int layout,
        [String] Reference altText, int appearanceMode)
    {
        base.Init();
        Label = label;
        Image = image;
        Layout = layout;
        AltText = altText;
        if (appearanceMode < 0 || appearanceMode > 2)
            Jvm.Throw<IllegalArgumentException>();
        Appearance = appearanceMode;
    }

    [return: String]
    public Reference getAltText() => AltText;

    public int getAppearanceMode() => Appearance;

    [return: JavaType(typeof(Image))]
    public Reference getImage() => Image;

    public void setAltText([String] Reference altText)
    {
        AltText = altText;
        NotifyToolkit();
    }

    public void setImage([JavaType(typeof(Image))] Reference image)
    {
        Image = image;
        NotifyToolkit();
    }
}
