using java.lang;
using MahoTrans.Runtime;

namespace MahoTrans.Toolkits;

public interface IClock
{
    /// <summary>
    /// Gets current time of the system. This is used for <see cref="System.currentTimeMillis"/>.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentMs(long currentTick);

    /// <summary>
    /// Gets current time of the system. This is used for thread wakeup management.
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in java format.</returns>
    long GetCurrentJvmMs(long currentTick);

    /// <summary>
    /// Gets amount of CLR-format time to wait when nothing is happening (i.e. all threads are dead)
    /// </summary>
    /// <param name="currentTick">Current jvm's tick.</param>
    /// <returns>Time in CLR format to pause thread for.</returns>
    long GetTicksToWait(long currentTick);

    /// <summary>
    /// Used to notify clock that jvm is going to sleep/wakeup
    /// </summary>
    bool JvmSleeping { set; }

    /// <summary>
    /// Gets CLR time in which ticks bunch must be done. Ticks count is set by <see cref="JvmState.CYCLES_PER_BUNCH"/>
    /// </summary>
    /// <returns></returns>
    long GetTicksPerCycleBunch();
}