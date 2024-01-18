// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.ToolkitImpls.Dummy;

public class DummyFonts : IFontManager
{
    public int GetFontHeight(FontSize size) => 20;

    public int GetCharWidth(FontFace face, FontStyle style, int size, char c) => 5;

    public int GetCharsWidth(FontFace face, FontStyle style, int size, ReadOnlySpan<char> c) => 5 * c.Length;
}