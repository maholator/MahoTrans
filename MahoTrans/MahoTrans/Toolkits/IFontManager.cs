// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;

namespace MahoTrans.Toolkits;

public interface IFontManager : IToolkit
{
    int GetFontHeight(FontSize size);

    int GetCharWidth(FontFace face, FontStyle style, int size, char c);

    int GetCharsWidth(FontFace face, FontStyle style, int size, ReadOnlySpan<char> c);
}