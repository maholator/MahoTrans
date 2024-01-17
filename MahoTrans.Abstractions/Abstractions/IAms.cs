// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that allows MIDlet to interact with AMS.
/// </summary>
public interface IAms : IToolkit
{
    /// <summary>
    ///     Notifies AMS that MIDlet pauses.
    /// </summary>
    void PauseMidlet();

    /// <summary>
    ///     Asks AMS to resume MIDlet.
    /// </summary>
    void ResumeMidlet();

    /// <summary>
    ///     Asks AMS to destroy MIDlet.
    /// </summary>
    void DestroyMidlet();

    /// <summary>
    ///     Performs "platform request" (i.e. opening an URL).
    /// </summary>
    /// <param name="url">URL to open.</param>
    void PlatformRequest(string url);
}