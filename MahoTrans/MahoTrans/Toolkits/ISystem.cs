// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;

namespace MahoTrans.Toolkits;

public interface ISystem : IToolkit
{
    void PrintException(Throwable t);

    void PrintOut(byte b);

    void PrintErr(byte b);

    string? GetProperty(string name);

    string TimeZone { get; }
}