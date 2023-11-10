using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace MahoTrans.ToolkitImpls.Loggers;

/// <summary>
/// Logger that does nothing with incoming messages.
/// </summary>
public class StubLogger : ILogger, ILoadTimeLogger, IHeapDebugger
{
    public void LogRuntime(LogLevel level, string message)
    {
    }

    public void LogDebug(DebugMessageCategory category, string message)
    {
    }

    public void Log(LogLevel level, string className, string message)
    {
    }

    public void ObjectCreated(Object obj)
    {
    }

    public void ObjectDeleted(Object obj)
    {
    }
}