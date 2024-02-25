// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Stub;

public class StubSystem : ISystem
{
    public void PrintException(Reference t)
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