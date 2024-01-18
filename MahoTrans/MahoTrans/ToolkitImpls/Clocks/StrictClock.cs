// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Clocks;

/// <summary>
///     Clock that locks JVM to specific tickrate and provides time based on it.
/// </summary>
public class StrictClock : IClock
{
    public long TicksPerBunch;
    public readonly long StartTicks;

    private readonly long _unixEpoch = DateTimeOffset.UnixEpoch.Ticks;

    private long _passedTime;

    /// <summary>
    ///     Initializes the clock.
    /// </summary>
    /// <param name="ticksPerBunch">CLR ticks for each cycles bunch.</param>
    /// <param name="startTicks">
    ///     CLR ticks of JVM start. To start right now, pass <see cref="DateTime.Now" />.
    ///     <see cref="DateTime.Ticks" />.
    /// </param>
    public StrictClock(int ticksPerBunch, long startTicks)
    {
        TicksPerBunch = ticksPerBunch;
        StartTicks = startTicks;
    }

    public long GetCurrentMs(long currentTick)
    {
        // this is passed time in CLR ticks since JVM start
        _passedTime = currentTick * TicksPerBunch / JvmState.CYCLES_PER_BUNCH;
        // this is a total passed time in global CLR ticks
        var absTick = _passedTime + StartTicks;
        // this is a time in CLR ticks since unix epoch
        var sinceUnix = (absTick - _unixEpoch);
        return sinceUnix / TimeSpan.TicksPerMillisecond;
    }

    public long GetCurrentJvmMs(long currentTick) => GetCurrentMs(currentTick);

    public bool JvmSleeping
    {
        set { }
    }

    public long CurrentTimeClr => _passedTime + StartTicks;

    public long PassedTimeClr => _passedTime;

    public long PassedTimeJvm => PassedTimeClr / TimeSpan.TicksPerMillisecond;

    public long GetTicksPerCycleBunch() => TicksPerBunch;
}