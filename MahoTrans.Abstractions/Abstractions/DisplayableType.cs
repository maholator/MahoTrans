// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Types of displayables.
/// </summary>
public enum DisplayableType
{
    /// <summary>
    ///     Canvas that is drawn using repaint events.
    /// </summary>
    EventBasedCanvas,

    /// <summary>
    ///     Canvas that is drawn by MIDlet itself.
    /// </summary>
    GameCanvas,
    Form,
    TextBox,
    List,
    Alert,
}