// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     List of controls, supported by media object.
/// </summary>
[Flags]
public enum MediaControls : uint
{
    FramePosition = 1 << 1,
    Gui = 1 << 2,
    MetaData = 1 << 3,
    Midi = 1 << 4,
    Pitch = 1 << 5,
    Rate = 1 << 6,
    Record = 1 << 7,
    StopTime = 1 << 8,
    Tempo = 1 << 9,
    Tone = 1 << 10,
    Video = 1 << 11,
    Volume = 1 << 12,
}
