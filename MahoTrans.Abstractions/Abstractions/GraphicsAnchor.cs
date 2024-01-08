// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Anchor constants from LCDUI.
/// </summary>
[Flags]
public enum GraphicsAnchor
{
    HCenter = 1,
    VCenter = 2,
    Left = 4,
    Right = 8,
    Top = 16,
    Bottom = 32,
    Baseline = 64,

    AllHorizontal = Left | HCenter | Right,
    AllVertical = Top | VCenter | Bottom | Baseline,
}