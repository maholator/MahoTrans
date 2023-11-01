namespace MahoTrans.Toolkits;

public interface ILogger
{
    public void LogLoadtime(LogLevel level, string className, string message);
    public void LogRuntime(LogLevel level, string message);
    public void LogDebug(DebugMessageCategory category, string message);
}