// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Loggers;

/// <summary>
///     Logger that does nothing with incoming messages.
/// </summary>
public class StubLogger : ILogger, ILoadLogger, IHeapDebugger
{
    public void LogRuntime(LogLevel level, string message)
    {
    }

    public void LogDebug(DebugMessageCategory category, string message)
    {
    }

    public void Log(LoadIssueType level, string className, string message)
    {
    }

    public void ObjectCreated(Reference obj)
    {
    }

    public void ObjectDeleted(Reference obj)
    {
    }

    public void SnapshotTaken()
    {
    }

    public void SnapshotRestored()
    {
    }
}
