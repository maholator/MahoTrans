// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Abstractions;

/// <summary>
///     Types of displayables.
/// </summary>
public enum DisplayableType
{
    /// <summary>
    ///     Canvas that is drawn using repaint events.
    /// </summary>
    [Description("Events-driven canvas")] EventBasedCanvas,

    /// <summary>
    ///     Canvas that is drawn by MIDlet itself.
    /// </summary>
    [Description("Application-driven canvas")]
    GameCanvas,

    [Description("LCDUI form")] Form,

    [Description("LCDUI text box")] TextBox,

    [Description("LCDUI list")] List,

    [Description("LCDUI alert")] Alert,
}