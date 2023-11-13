using MahoTrans.Toolkits;

namespace MahoTrans.Runtime;

/// <summary>
/// Sets kind of loop in which interpreter will run.
/// </summary>
public enum ExecutionManner
{
    /// <summary>
    /// JVM will run in endless loop as fast as possible.
    /// </summary>
    Unlocked,

    /// <summary>
    /// JVM will sync its execution speed with <see cref="IClock.GetTicksPerCycleBunch"/> each bunch.
    /// Spinwait will be used. This is the most accurate way but it will fully use one CPU core as <see cref="Unlocked"/>.
    /// </summary>
    Strict,

    /// <summary>
    /// JVM will sync its execution speed with <see cref="IClock.GetTicksPerCycleBunch"/> each bunch.
    /// If jvm is running faster than needed it will pause for a while. This prevents 100% cpu usage but less accurate.
    /// </summary>
    Weak
}