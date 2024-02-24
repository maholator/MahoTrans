// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that allows MIDlet and JVM to obtain current time.
/// </summary>
public abstract class Clock : IToolkit
{
    #region JVM-side APIs

    /// <summary>
    ///     Gets current time of the system. This is used for System.currentTimeMillis() calls.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in java format.</returns>
    public abstract long GetCurrentMs(long currentCycle);

    /// <summary>
    ///     Gets current time of the system. This is used for thread management.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in java format.</returns>
    public abstract long GetCurrentJvmMs(long currentCycle);

    /// <summary>
    ///     Gets current time of the system in CLR format.
    /// </summary>
    /// <param name="currentCycle">Сurrent jvm's cycle.</param>
    /// <returns>Time in CLR format.</returns>
   public abstract  long GetCurrentClrTicks(long currentCycle);

    /// <summary>
    ///     Gets time, passed since JVM start in CLR format.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in CLR format.</returns>
   public abstract  long GetPassedClrTicks(long currentCycle);

    /// <summary>
    ///     CLR time in which cycles step must be done. Ticks count is set by JvmState.CYCLES_PER_BUNCH. This must be managed by the clock.
    /// </summary>
    public long TicksPerCycleStep;


    #endregion
}