using java.lang;
using MahoTrans.Runtime;

namespace MahoTrans.Toolkits;

public interface IClock : IToolkit
{
    #region JVM-side APIs

    /// <summary>
    /// Gets current time of the system. This is used for <see cref="System.currentTimeMillis"/>.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentMs(long currentTick);

    /// <summary>
    /// Gets current time of the system. This is used for thread management.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentJvmMs(long currentTick);

    /// <summary>
    /// Gets CLR time in which ticks bunch must be done. Ticks count is set by <see cref="JvmState.CYCLES_PER_BUNCH"/>
    /// </summary>
    /// <returns>CLR ticks.</returns>
    long GetTicksPerCycleBunch();

    /// <summary>
    /// Used to notify clock that jvm is going to sleep/wakeup
    /// </summary>
    bool JvmSleeping { set; }

    #endregion

    #region Frontend-side APIs

    /// <summary>
    /// Gets current time of the system. Returned time is in CLR format.
    /// </summary>
    long CurrentTime { get; }

    /// <summary>
    /// Gets current time of the system since JVM start. Returned time is in CLR format.
    /// </summary>
    long PassedTime { get; }

    #endregion
}