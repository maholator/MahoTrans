using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Loggers;

/// <summary>
/// Logger that writes all messages to console.
/// </summary>
public class ConsoleLogger : ILogger, ILoadTimeLogger
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
}