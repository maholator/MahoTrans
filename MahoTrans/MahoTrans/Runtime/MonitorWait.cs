namespace MahoTrans.Runtime;

public readonly struct MonitorWait
{
    public readonly uint MonitorReEnterCount;
    public readonly int MonitorOwner;

    public MonitorWait(uint monitorReEnterCount, int monitorOwner)
    {
        MonitorReEnterCount = monitorReEnterCount;
        MonitorOwner = monitorOwner;
    }

    public static implicit operator long(MonitorWait mw)
    {
        return (long)((ulong)mw.MonitorReEnterCount << 32 | (uint)mw.MonitorOwner);
    }

    public static implicit operator MonitorWait(long packed)
    {
        ulong up = (ulong)packed;
        uint count = (uint)(up >> 32);
        int owner = (int)(uint)up;
        return new MonitorWait(count, owner);
    }
}