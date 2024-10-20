// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Stub;

/// <summary>
///     Logger that does nothing with incoming messages.
/// </summary>
public class StubLogger : ILogger, ILoadLogger, IHeapDebugger
{
    public void LogRuntime(MTLogLevel level, string message)
    {
    }

    public void LogEvent(EventCategory category, string message)
    {
    }

    public void Log(LoadIssueType level, string className, string message)
    {
    }

    public void ReportLinkProgress(int num, int total, string name)
    {
    }

    public void ReportCompileProgress(int num, int total, string name)
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

    public void LogExceptionThrow(Reference t)
    {
    }

    public void LogExceptionCatch(Reference t)
    {
    }
}
