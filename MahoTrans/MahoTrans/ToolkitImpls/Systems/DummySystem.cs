// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Systems;

public class DummySystem : ISystem
{
    public void PrintException(Throwable t)
    {
    }

    public void PrintOut(byte b)
    {
    }

    public void PrintErr(byte b)
    {
    }

    public string? GetProperty(string name)
    {
        return "Unknown";
    }

    public string TimeZone => "UTC";
}