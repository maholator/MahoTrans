// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime;

/// <summary>
///     Struct which keeps info what thread to wake up and when.
/// </summary>
public readonly struct ThreadWakeupHook
{
    public readonly long WakeupAtMs;

    /// <summary>
    ///     Thread that waits for wakeup.
    /// </summary>
    public readonly int ThreadId;

    /// <summary>
    ///     Object where this thread waits for notify.
    /// </summary>
    public readonly Reference MonitorObject;

    public ThreadWakeupHook(long wakeupAtMs, int threadId, Reference monitorObject)
    {
        WakeupAtMs = wakeupAtMs;
        ThreadId = threadId;
        MonitorObject = monitorObject;
    }
}
