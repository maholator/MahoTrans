// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that provides font metrics.
/// </summary>
public interface IFontManager : IToolkit
{
    /// <summary>
    ///     Returns values for Font.getHeight().
    /// </summary>
    /// <param name="size">Font size as LCDUI constant.</param>
    /// <returns>Height of text line in pixels.</returns>
    int GetFontHeight(FontSize size);

    /// <summary>
    ///     Returns values for Font.charWidth().
    /// </summary>
    /// <param name="face">Font face as LCDUI constant.</param>
    /// <param name="style">Font style as LCDUI constant.</param>
    /// <param name="size">Font size as LCDUI constant.</param>
    /// <param name="c">Character to measure.</param>
    /// <returns>Width of character in pixels.</returns>
    int GetCharWidth(FontFace face, FontStyle style, int size, char c);

    /// <summary>
    ///     Returns values for Font.getCharsWidth().
    /// </summary>
    /// <param name="face">Font face as LCDUI constant.</param>
    /// <param name="style">Font style as LCDUI constant.</param>
    /// <param name="size">Font size as LCDUI constant.</param>
    /// <param name="c">Characters to measure.</param>
    /// <returns>Width of character sequence in pixels.</returns>
    int GetCharsWidth(FontFace face, FontStyle style, int size, ReadOnlySpan<char> c);
}
