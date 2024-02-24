// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Clocks;

/// <summary>
///     Clock that locks JVM to specific tickrate and provides time based on it.
/// </summary>
public class StrictClock : Clock
{
    public readonly long StartTicks;

    private static readonly long UnixEpoch = DateTimeOffset.UnixEpoch.Ticks;

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
        TicksPerCycleStep = ticksPerBunch;
        StartTicks = startTicks;
    }

    public override long GetCurrentMs(long currentTick)
    {
        // this is passed time in CLR ticks since JVM start
        var passedTime = currentTick * TicksPerCycleStep / JvmState.CYCLES_PER_BUNCH;
        // this is a total passed time in global CLR ticks
        var absTick = passedTime + StartTicks;
        // this is a time in CLR ticks since unix epoch
        var sinceUnix = (absTick - UnixEpoch);
        return sinceUnix / TimeSpan.TicksPerMillisecond;
    }

    public override long GetCurrentJvmMs(long currentTick) => GetCurrentMs(currentTick);

    public override long GetCurrentClrTicks(long currentCycle)
    {
        // this is passed time in CLR ticks since JVM start
        var passedTime = currentCycle * TicksPerCycleStep / JvmState.CYCLES_PER_BUNCH;
        // this is a total passed time in global CLR ticks
        var absTick = passedTime + StartTicks;

        return absTick;
    }

    public override long GetPassedClrTicks(long currentCycle)
    {
        return currentCycle * TicksPerCycleStep / JvmState.CYCLES_PER_BUNCH;
    }
}