namespace MahoTrans.Toolkits;

public interface ILogger
{
    public void PrintLoadTime(LogLevel level, string className, string message);
    public void PrintRuntime(LogLevel level, string message);
    public void PrintDebug(DebugMessageCategory category, string message);
}