// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.Contracts;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that allows MIDlet and JVM to obtain current time.
/// </summary>
public interface IClock : IToolkit
{
    #region JVM-side APIs

    /// <summary>
    ///     Gets current time of the system. This is used for System.currentTimeMillis() calls.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentMs(long currentCycle);

    /// <summary>
    ///     Gets current time of the system. This is used for thread management.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentJvmMs(long currentCycle);

    /// <summary>
    ///     Gets current time of the system in CLR format.
    /// </summary>
    /// <param name="currentCycle">Сurrent jvm's cycle.</param>
    /// <returns>Time in CLR format.</returns>
    long GetCurrentClrTicks(long currentCycle);

    /// <summary>
    ///     Gets time, passed since JVM start in CLR format.
    /// </summary>
    /// <param name="currentCycle">Current jvm's cycle.</param>
    /// <returns>Time in CLR format.</returns>
    long GetPassedClrTicks(long currentCycle);

    /// <summary>
    ///     Gets CLR time in which ticks bunch must be done. Ticks count is set by JvmState.CYCLES_PER_BUNCH.
    /// </summary>
    [Pure]
    long TicksPerCycleBunch { get; }

    /// <summary>
    ///     Used to notify clock that jvm is going to sleep/wakeup
    /// </summary>
    bool JvmSleeping { set; }

    #endregion
}