// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that allows MIDlet to interact with frontend.
/// </summary>
public interface IAmsCallbacks : IToolkit
{
    /// <summary>
    ///     Fires when MIDlet enters "paused" state. This is called both when midlet pauses itself or when frontend does it. At this moment, midlet's
    /// </summary>
    void MidletPaused();

    /// <summary>
    ///     Fires when midlet wants to resume
    /// </summary>
    void AskForResume();

    /// <summary>
    ///     Fires on exit() call.
    /// </summary>
    void Exited(int code);

    /// <summary>
    ///     Performs "platform request" (i.e. opening an URL).
    /// </summary>
    /// <param name="url">URL to open.</param>
    void PlatformRequest(string url);
}