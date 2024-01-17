// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that implements java.lang.System.
/// </summary>
public interface ISystem : IToolkit
{
    /// <summary>
    ///     Prints exception to stderr. To print exception to log, use <see cref="ILogger.LogExceptionThrow" />.
    /// </summary>
    /// <param name="t">Exception to print.</param>
    void PrintException(Reference t);

    /// <summary>
    ///     Standard output stream prints here.
    /// </summary>
    /// <param name="b">Byte to write.</param>
    void PrintOut(byte b);

    /// <summary>
    ///     Standard error stream prints here.
    /// </summary>
    /// <param name="b">Byte to write.</param>
    void PrintErr(byte b);

    /// <summary>
    ///     Gets system property.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <returns>Behaviour is stated in CLDC docs.</returns>
    string? GetProperty(string name);

    /// <summary>
    ///     Gets timezone, for example, "GMT".
    /// </summary>
    string TimeZone { get; }
}