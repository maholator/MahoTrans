// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Toolkits;

public interface ILogger
{
    public void LogRuntime(LogLevel level, string message);
    public void LogDebug(DebugMessageCategory category, string message);
}