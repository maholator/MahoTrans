// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.ToolkitImpls.Clocks;

/// <summary>
///     Simple clock. Allows midlet to run as fast as possible and gives it real time information.
/// </summary>
public class RealTimeClock : Clock
{
    private long _startTick;

    public RealTimeClock()
    {
        TicksPerCycleStep = 0;
    }

    public override void Init()
    {
        _startTick = DateTime.Now.Ticks;
    }

    public override long GetCurrentMs(long currentTick)
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public override long GetCurrentJvmMs(long currentTick) => GetCurrentMs(currentTick);

    public override long GetCurrentClrTicks(long currentCycle) => DateTime.Now.Ticks;

    public override long GetPassedClrTicks(long currentCycle) => DateTime.Now.Ticks - _startTick;
}
