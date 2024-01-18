// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Font style constants from LCDUI.
/// </summary>
[Flags]
public enum FontStyle
{
    Plain = 0,
    Bold = 1,
    Italic = 2,
    Underlined = 4,
}