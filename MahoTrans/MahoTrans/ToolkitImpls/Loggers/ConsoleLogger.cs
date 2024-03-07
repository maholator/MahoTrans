// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

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

    public void LogRuntime(MTLogLevel level, string message)
    {
        var c = Console.ForegroundColor;
        SetConsoleColor(level);
        Console.Write(message);
        Console.ForegroundColor = c;
        Console.WriteLine();
    }

    public void LogEvent(EventCategory category, string message)
    {
        Console.WriteLine($"{category}: {message}");
    }

    private static void SetConsoleColor(MTLogLevel level)
    {
        Console.ForegroundColor = level switch
        {
            MTLogLevel.Info => ConsoleColor.Green,
            MTLogLevel.Warning => ConsoleColor.Yellow,
            MTLogLevel.Error => ConsoleColor.Red,
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
