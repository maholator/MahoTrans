// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.lcdui;

public class Font : Object
{
    [JavaIgnore] public FontFace Face;

    [JavaIgnore] public FontStyle Style;

    /// <summary>
    /// Font size as LCDUI constant. For custom fonts, this is 0.
    /// </summary>
    [JavaIgnore] public FontSize Size;

    /// <summary>
    /// Height of the font in pixels.
    /// </summary>
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

    public int getStyle() => (int)Style;

    public int getFace() => (int)Face;

    public bool isPlain() => Style == FontStyle.Plain;

    public bool isBold() => (Style & FontStyle.Bold) != 0;

    public bool isItalic() => (Style & FontStyle.Italic) != 0;

    public bool isUnderlined() => (Style & FontStyle.Underlined) != 0;

    public int stringWidth([String] Reference str)
    {
        return Toolkit.Fonts.GetCharsWidth(Face, Style, Height, Jvm.ResolveString(str));
    }

    public int substringWidth([String] Reference str, int from, int len)
    {
        return Toolkit.Fonts.GetCharsWidth(Face, Style, Height,
            Jvm.ResolveString(str).AsSpan(from, len));
    }

    public int charWidth(char c)
    {
        return Toolkit.Fonts.GetCharWidth(Face, Style, Height, c);
    }

    public int charsWidth([JavaType("[C")] Reference str, int from, int len)
    {
        return Toolkit.Fonts.GetCharsWidth(Face, Style, Height,
            Jvm.ResolveArray<char>(str).AsSpan(from, len));
    }


    public const int FACE_MONOSPACE = 32;
    public const int FACE_PROPORTIONAL = 64;
    public const int FACE_SYSTEM = 0;
    public const int FONT_INPUT_TEXT = 1;
    public const int FONT_STATIC_TEXT = 0;
    public const int SIZE_LARGE = 16;
    public const int SIZE_MEDIUM = 0;
    public const int SIZE_SMALL = 8;
    public const int STYLE_BOLD = 1;
    public const int STYLE_ITALIC = 2;
    public const int STYLE_PLAIN = 0;
    public const int STYLE_UNDERLINED = 4;
}