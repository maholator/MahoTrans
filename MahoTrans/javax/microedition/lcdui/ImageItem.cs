using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ImageItem : Item
{
    [JavaType(typeof(Image))] public Reference Image;
    [String] public Reference AltText;
    public int Appearance;

    [InitMethod]
    public void Init([String] Reference label, [JavaType(typeof(Image))] Reference image, int layout,
        [String] Reference altText)
    {
        base.Init();
        Label = label;
        Image = image;
        //todo layout
        AltText = altText;
    }

    [InitMethod]
    public void Init([String] Reference label, [JavaType(typeof(Image))] Reference image, int layout,
        [String] Reference altText, int appearanceMode)
    {
        base.Init();
        Label = label;
        Image = image;
        //todo layout
        AltText = altText;
        if (appearanceMode < 0 || appearanceMode > 2)
            Jvm.Throw<IllegalArgumentException>();
        Appearance = appearanceMode;
    }

    public void setImage([JavaType(typeof(Image))] Reference image)
    {
        Image = image;
        NotifyToolkit();
    }

    [return: JavaType(typeof(Image))]
    public Reference getImage() => Image;
}