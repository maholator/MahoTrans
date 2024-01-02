// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime;

public readonly struct ThreadWakeupHook
{
    public readonly long WakeupAtMs;
    public readonly int ThreadId;

    public ThreadWakeupHook(long wakeupAtMs, int threadId)
    {
        WakeupAtMs = wakeupAtMs;
        ThreadId = threadId;
    }
}