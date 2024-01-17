// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
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
    ///     Gets current time of the system. This is used for System.currentTimeMillis() calls. This should update values, returned by <see cref="PassedTimeClr"/>/<see cref="PassedTimeJvm"/> props.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentMs(long currentTick);

    /// <summary>
    ///     Gets current time of the system. This is used for thread management. This MUST update values, returned by <see cref="PassedTimeClr"/>/<see cref="PassedTimeJvm"/> props.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentJvmMs(long currentTick);

    /// <summary>
    ///     Gets CLR time in which ticks bunch must be done. Ticks count is set by JvmState.CYCLES_PER_BUNCH.
    /// </summary>
    /// <returns>CLR ticks.</returns>
    [Pure]
    long GetTicksPerCycleBunch();

    /// <summary>
    ///     Used to notify clock that jvm is going to sleep/wakeup
    /// </summary>
    bool JvmSleeping { set; }

    #endregion

    #region Frontend-side APIs

    /// <summary>
    ///     Gets current time of the system. Returned time is in CLR format.
    /// </summary>
    [Pure]
    long CurrentTimeClr { get; }

    /// <summary>
    ///     Gets current time of the system since JVM start. Returned time is in CLR format (CLR ticks). This is updated by <see cref="GetCurrentJvmMs"/> calls and actually a last returned time.
    /// </summary>
    [Pure]
    long PassedTimeClr { get; }

    /// <summary>
    ///     Gets current time of the system since JVM start. Returned time is in JVM format (milliseconds). This is updated by <see cref="GetCurrentJvmMs"/> calls and actually a last returned time.
    /// </summary>
    [Pure]
    long PassedTimeJvm { get; }

    #endregion
}