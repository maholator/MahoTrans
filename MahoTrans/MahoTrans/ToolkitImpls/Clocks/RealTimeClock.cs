using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Clocks;

/// <summary>
/// Simple clock. Allows midlet to run as fast as possible and gives it real time information.
/// </summary>
public class RealTimeClock : IClock
{
    private long _startTick;

    public long GetCurrentMs(long currentTick)
    {
        if (_startTick == 0)
            _startTick = DateTime.Now.Ticks;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public long GetCurrentJvmMs(long currentTick)
    {
        return GetCurrentMs(currentTick);
    }

    public bool JvmSleeping
    {
        set { }
    }

    public long CurrentTime => DateTime.Now.Ticks;

    public long PassedTime => DateTime.Now.Ticks - _startTick;

    public long GetTicksPerCycleBunch() => 0;
}