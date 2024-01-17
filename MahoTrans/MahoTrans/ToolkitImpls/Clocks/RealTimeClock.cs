// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.ToolkitImpls.Clocks;

/// <summary>
///     Simple clock. Allows midlet to run as fast as possible and gives it real time information.
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

    public long CurrentTimeClr => DateTime.Now.Ticks;

    public long PassedTimeClr => DateTime.Now.Ticks - _startTick;

    public long PassedTimeJvm => PassedTimeClr / TimeSpan.TicksPerMillisecond;

    public long GetTicksPerCycleBunch() => 0;
}