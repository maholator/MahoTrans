// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Image manipulation flags from NokiaUI.
/// </summary>
[Flags]
public enum ImageManipulation
{
    FlipHorizontal = 8192,
    FlipVertical = 16384,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270,
}