// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Handles;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Stub;

/// <summary>
///     Display "implementation" that throws on any call.
/// </summary>
//TODO add proper stub
public class NotImplementedDisplay : IDisplay
{
    public DisplayableHandle Register(Reference d)
    {
        throw new NotImplementedException();
    }

    public void SetCurrent(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public void SetNullCurrent()
    {
        throw new NotImplementedException();
    }

    public int GetWidth(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public int GetHeight(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public DisplayableType GetType(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public void SetFullscreen(DisplayableHandle handle, bool state)
    {
        throw new NotImplementedException();
    }

    public void SetTitle(DisplayableHandle handle, string? title)
    {
        throw new NotImplementedException();
    }

    public void CommandsUpdated(DisplayableHandle handle, List<Reference> commands, Reference selectCommand)
    {
        throw new NotImplementedException();
    }

    public void TickerUpdated()
    {
        throw new NotImplementedException();
    }

    public void ContentUpdated(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public void ItemUpdated(DisplayableHandle displayable, Reference item)
    {
        throw new NotImplementedException();
    }

    public void FocusItem(DisplayableHandle displayable, Reference item)
    {
        throw new NotImplementedException();
    }

    public GraphicsHandle GetGraphics(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public void Flush(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public void Flush(DisplayableHandle handle, int x, int y, int width, int height)
    {
        throw new NotImplementedException();
    }

    public void Release(DisplayableHandle handle)
    {
        throw new NotImplementedException();
    }

    public DisplayableHandle? Current => throw new NotImplementedException();
}