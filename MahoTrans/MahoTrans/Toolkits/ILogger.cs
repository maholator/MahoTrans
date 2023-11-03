namespace MahoTrans.Toolkits;

public interface ILogger
{
    public void LogRuntime(LogLevel level, string message);
    public void LogDebug(DebugMessageCategory category, string message);
}