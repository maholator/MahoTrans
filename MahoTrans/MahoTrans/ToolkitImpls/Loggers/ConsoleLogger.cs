// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Abstractions;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.ToolkitImpls.Loggers;

/// <summary>
///     Logger that writes all messages to console.
/// </summary>
public class ConsoleLogger : ILogger, ILoadLogger
{
    public void Log(LoadIssueType level, string className, string message)
    {
        Console.Write(className);
        Console.Write(": ");
        Console.Write(message);
        Console.WriteLine();
    }

    public void LogRuntime(LogLevel level, string message)
    {
        var c = Console.ForegroundColor;
        SetConsoleColor(level);
        Console.Write(message);
        Console.ForegroundColor = c;
        Console.WriteLine();
    }

    public void LogDebug(DebugMessageCategory category, string message)
    {
        Console.WriteLine($"{category}: {message}");
    }

    private static void SetConsoleColor(LogLevel level)
    {
        Console.ForegroundColor = level switch
        {
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
    }

    public void LogExceptionThrow(Reference t)
    {
        //var e = t.As<Throwable>();
        //LogDebug(DebugMessageCategory.Exceptions, $"Exception {e.JavaClass.Name} is thrown via native");
    }

    public void LogExceptionCatch(Reference t)
    {
        //var e = t.As<Throwable>();
        //LogDebug(DebugMessageCategory.Exceptions, $"Exception {e.JavaClass.Name} is caught");
    }
}