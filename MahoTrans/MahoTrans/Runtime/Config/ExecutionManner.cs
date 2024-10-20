// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.Runtime.Config;

/// <summary>
///     Sets kind of loop in which interpreter will run.
/// </summary>
public enum ExecutionManner
{
    /// <summary>
    ///     JVM will run in endless loop as fast as possible.
    /// </summary>
    Unlocked,

    /// <summary>
    ///     JVM will sync its execution speed with <see cref="Clock.GetTicksPerCycleBunch" /> each bunch.
    ///     Spinwait will be used. This is the most accurate way but it will fully use one CPU core as <see cref="Unlocked" />.
    /// </summary>
    Strict,

    /// <summary>
    ///     JVM will sync its execution speed with <see cref="Clock.GetTicksPerCycleBunch" /> each bunch.
    ///     If jvm is running faster than needed it will pause for a while. This prevents 100% cpu usage but less accurate.
    /// </summary>
    Weak
}
