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