// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace javax.microedition.lcdui;

[Flags]
public enum ImageManipulation
{
    FlipHorizontal = 8192,
    FlipVertical = 16384,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270,
}