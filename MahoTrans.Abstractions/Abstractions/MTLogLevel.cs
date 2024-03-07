// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Importance of log message.
/// </summary>
/// <remarks>
///     This used to be just a "LogLevel", but EVERY single library brings its own "log level" enum so this was renamed to
///     reduce naming conflicts.
/// </remarks>
public enum MTLogLevel
{
    /// <summary>
    ///     Informational message.
    /// </summary>
    Info = 0,

    /// <summary>
    ///     Warning message.
    /// </summary>
    Warning = 1,

    /// <summary>
    ///     Error message.
    /// </summary>
    Error = 2,
}
