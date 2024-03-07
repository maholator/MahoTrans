// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
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
    [Description("Events-driven canvas")]
    EventBasedCanvas,

    /// <summary>
    ///     Canvas that is drawn by MIDlet itself.
    /// </summary>
    [Description("Application-driven canvas")]
    GameCanvas,

    /// <summary>
    ///     LCDUI form.
    /// </summary>
    [Description("LCDUI form")]
    Form,

    /// <summary>
    ///     LCDUI text box.
    /// </summary>
    [Description("LCDUI text box")]
    TextBox,

    /// <summary>
    ///     LCDUI list.
    /// </summary>
    [Description("LCDUI list")]
    List,

    /// <summary>
    ///     LCDUI alert.
    /// </summary>
    [Description("LCDUI alert")]
    Alert,
}
