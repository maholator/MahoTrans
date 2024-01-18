// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;
using MahoTrans.Runtime;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit which is responsible for displaying midlet's UI in emulator's UI.
/// </summary>
public interface IDisplay : IToolkit
{
    /// <summary>
    ///     Notifies toolkit about newly constructed displayable.
    /// </summary>
    /// <param name="d">Object of created displayable. Toolkit must not store it in any way.</param>
    /// <returns>Handle for communicating with the toolkit.</returns>
    DisplayableHandle Register(Reference d);

    /// <summary>
    ///     Sets the displayable as current on display.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    void SetCurrent(DisplayableHandle handle);

    /// <summary>
    ///     Removes a displayable from display.
    /// </summary>
    void SetNullCurrent();

    /// <summary>
    ///     Gets current displayable on display.
    /// </summary>
    public DisplayableHandle? Current { get; }

    /// <summary>
    ///     Gets width of the displayable.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <returns>Width of the displayable in pixels.</returns>
    int GetWidth(DisplayableHandle handle);

    /// <summary>
    ///     Gets height of the displayable.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <returns>Height of the displayable in pixels.</returns>
    int GetHeight(DisplayableHandle handle);

    /// <summary>
    ///     Gets type of the displayable.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <returns>Displayable's type.</returns>
    DisplayableType GetType(DisplayableHandle handle);

    /// <summary>
    ///     Sets displayable's fullscreen mode. This throws if displayable can't be in fullscreen mode.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <param name="state">State to set.</param>
    void SetFullscreen(DisplayableHandle handle, bool state);

    /// <summary>
    ///     Notifies toolkit about displayable's title change.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <param name="title">New title as CLR string.</param>
    void SetTitle(DisplayableHandle handle, string? title);

    /// <summary>
    ///     Notifies toolkit that commands list on the displayable was changed.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <param name="commands">
    ///     New list of commands. List is unsorted.
    ///     All objects are guaranteed to be Commands.
    ///     Implicit list's select command must not be in this list.
    ///     This list must not contain any commands for items.
    /// </param>
    /// <param name="selectCommand">For implicit list - select command. For other screens this must be zero.</param>
    void CommandsUpdated(DisplayableHandle handle, List<Reference> commands, Reference selectCommand);

    /// <summary>
    ///     Notifies toolkit that ticker was updated.
    /// </summary>
    void TickerUpdated();

    /// <summary>
    ///     Notifies toolkit that screen displayable changed its content (text, subitems, etc.).
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <remarks>
    ///     Title changes are reported via <see cref="SetTitle" />.<br />
    ///     Fullscreen mode is set via <see cref="SetFullscreen" />.<br />
    ///     Commands changes are reported via <see cref="CommandsUpdated" />.<br />
    ///     Framebuffer changes are reported via <see cref="Flush(DisplayableHandle)" />.<br />
    ///     Ticker changes are reported via <see cref="TickerUpdated" />.<br />
    ///     This is used only for LCDUI things (alert text/image, list items, form items, textbox content).
    /// </remarks>
    void ContentUpdated(DisplayableHandle handle);


    /// <summary>
    ///     Gets graphics to draw on the displayable. If it's not possible to draw on the displayable, this will throw.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <returns>Handle of graphics object.</returns>
    GraphicsHandle GetGraphics(DisplayableHandle handle);

    /// <summary>
    ///     Flushes contents of the displayable. If displayable is not something that can be flushed, this does nothing.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    void Flush(DisplayableHandle handle);

    /// <summary>
    ///     Flushes part of the displayable. If displayable is not something that can be flushed, this does nothing.
    /// </summary>
    /// <param name="handle">Displayable's handle.</param>
    /// <param name="x">X.</param>
    /// <param name="y">Y.</param>
    /// <param name="width">Area width.</param>
    /// <param name="height">Area height.</param>
    void Flush(DisplayableHandle handle, int x, int y, int width, int height);

    /// <summary>
    ///     Notifies toolkit that this displayable is dead.
    /// </summary>
    /// <param name="handle">Displayable's handle. It must not be used anymore.</param>
    void Release(DisplayableHandle handle);
}