// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that allows JVM and MIDlet to tell frontend what's going on.
/// </summary>
public interface ILogger
{
    /// <summary>
    ///     Logs arbitrary message.
    /// </summary>
    /// <param name="level">Importance of the message.</param>
    /// <param name="message">Message to log.</param>
    void LogRuntime(MTLogLevel level, string message);

    /// <summary>
    ///     Logs event message.
    /// </summary>
    /// <param name="category">Category of the message.</param>
    /// <param name="message">Text of the message.</param>
    void LogEvent(EventCategory category, string message);

    void LogExceptionThrow(Reference t);

    void LogExceptionCatch(Reference t);
}