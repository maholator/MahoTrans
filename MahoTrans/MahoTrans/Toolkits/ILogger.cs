using JetBrains.Annotations;

namespace MahoTrans.Toolkits;

public interface ILogger
{
    public void PrintLoadTime(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args);
    public void PrintRuntime(LogLevel level, [StructuredMessageTemplate] string message, params object?[] args);
    public void PrintDebug(DebugMessageCategory category, [StructuredMessageTemplate] string message, params object?[] args);
}