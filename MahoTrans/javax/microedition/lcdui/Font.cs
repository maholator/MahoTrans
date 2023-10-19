using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Font : Object
{
    [JavaIgnore] public FontFace Face;

    [JavaIgnore] public FontStyle Style;

    [JavaIgnore] public FontSize Size;

    [JavaIgnore] public int Height;

    [return: JavaType(typeof(Font))]
    public static Reference getFont(int face, int style, int size)
    {
        //TODO checks
        var font = Jvm.AllocateObject<Font>();
        font.Face = (FontFace)face;
        font.Style = (FontStyle)style;
        font.Size = (FontSize)size;
        font.Height = Toolkit.Fonts.GetFontHeight(font.Size);
        return font.This;
    }

    [return: JavaType(typeof(Font))]
    public static Reference getDefaultFont()
    {
        var font = Jvm.AllocateObject<Font>();
        font.Face = 0;
        font.Style = 0;
        font.Size = FontSize.Medium;
        font.Height = Toolkit.Fonts.GetFontHeight(font.Size);
        return font.This;
    }

    public int getHeight() => Height;

    public int getBaselinePosition() => Height;

    public int getSize() => (int)Size;

    public int stringWidth([String] Reference str)
    {
        return Toolkit.Fonts.GetCharsWidth(Face, Style, Height, Jvm.ResolveString(str));
    }

    public int substringWidth([String] Reference str, int from, int len)
    {
        return Toolkit.Fonts.GetCharsWidth(Face, Style, Height,
            Jvm.ResolveString(str).Skip(from).Take(len).ToArray());
    }

    public int charWidth(char c)
    {
        return Toolkit.Fonts.GetCharWidth(Face, Style, Height, c);
    }
}