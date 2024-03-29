// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Sprite transform constants from LCDUI.
/// </summary>
public enum SpriteTransform
{
    None = 0,
    Rot90 = 5,
    Rot180 = 3,
    Rot270 = 6,

    Mirror = 2,
    MirrorRot90 = 7,
    MirrorRot180 = 1,
    MirrorRot270 = 4,
}
