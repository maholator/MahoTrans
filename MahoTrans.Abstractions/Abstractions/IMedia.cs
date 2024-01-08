// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that enables audio/video playback.
/// </summary>
public interface IMedia : IToolkit
{
    #region Manager APIs

    /// <summary>
    ///     Plays MIDI tone. See MIDP docs.
    /// </summary>
    /// <param name="note">Note to play.</param>
    /// <param name="duration">Duration of the tone.</param>
    /// <param name="volume">Volume of the tone.</param>
    void PlayTone(int note, int duration, int volume);

    /// <summary>
    ///     Creates media player from file.
    /// </summary>
    /// <param name="data">File content. It will be copied so pass it as directly as possible.</param>
    /// <param name="contentType">Content type.</param>
    /// <param name="callbacks">Callbacks to use.</param>
    /// <returns>Media handle.</returns>
    MediaHandle Create(Memory<sbyte> data, string contentType, IMediaCallbacks callbacks);

    /// <summary>
    ///     Creates media player from MRL.
    /// </summary>
    /// <param name="mrl">MRL to play.</param>
    /// <param name="callbacks">Callbacks to use.</param>
    /// <returns>Media handle.</returns>
    MediaHandle Create(string mrl, IMediaCallbacks callbacks);

    /// <summary>
    ///     Returns list of supported content types.
    /// </summary>
    string[] GetSupportedContentTypes(string protocol);

    /// <summary>
    ///     Returns list of supported protocols.
    /// </summary>
    string[] GetSupportedProtocols(string contentType);

    #endregion

    #region Player APIs

    /// <summary>
    ///     Asks toolkit to bring player into ready state. Equivalent to prefetch() call. realize() call must do nothing.
    /// </summary>
    /// <param name="media">Media to prefetch.</param>
    void Prefetch(MediaHandle media);

    /// <summary>
    ///     Asks toolkit to stop and destroy the player. This must not be called twice.
    ///     This is equivalent to close() call. If close() was not called, this must be called on player object destroy.
    ///     deallocate() call must do nothing.
    /// </summary>
    /// <param name="media">Media to dispose.</param>
    void Dispose(MediaHandle media);

    /// <summary>
    ///     Gets media content type. See MIDP docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>Content type.</returns>
    string GetContentType(MediaHandle media);

    /// <summary>
    ///     Starts playback.
    /// </summary>
    /// <param name="media">Media handle.</param>
    void Start(MediaHandle media);

    /// <summary>
    ///     Stops playback.
    /// </summary>
    /// <param name="media">Media handle.</param>
    void Stop(MediaHandle media);

    /// <summary>
    ///     Gets duration of the media. See MIDP docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>As per MIDP docs.</returns>
    long GetDuration(MediaHandle media);

    /// <summary>
    ///     Gets current time of the media. See MIDP docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>As per MIDP docs.</returns>
    long GetTime(MediaHandle media);

    /// <summary>
    ///     Sets current time of the media. See MIDP docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <param name="time">Time to set. See MIDP docs.</param>
    void SetTime(MediaHandle media, long time);

    /// <summary>
    ///     Sets loop count.
    /// </summary>
    /// <param name="count">Loop count. -1 to infinite. 1 to one time. Zero is invalid.</param>
    void SetLoopCount(int count);

    #endregion

    #region Control APIs

    /// <summary>
    ///     Gets playback volume.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>Volume.</returns>
    int GetVolume(MediaHandle media);

    /// <summary>
    ///     Sets playback volume.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <param name="volume">Zero to mute, 100 to full. Negative or bigger values will be clamped.</param>
    void SetVolume(MediaHandle media, int volume);

    /// <summary>
    ///     Gets mute state of the player.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>True, if playback is muted.</returns>
    bool GetMute(MediaHandle media);

    /// <summary>
    ///     Sets mute state of the player.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <param name="mute">True to mute.</param>
    void SetMute(MediaHandle media, bool mute);

    /// <summary>
    ///     Gets time when playback will stop. See MMAPI docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>Time when playback will stop. See MMAPI docs.</returns>
    long GetStopTime(MediaHandle media);

    /// <summary>
    ///     Sets time when playback will stop. See MMAPI docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <param name="time">Time when playback will stop. See MMAPI docs.</param>
    void SetStopTime(MediaHandle media, long time);

    /// <summary>
    ///     Gets minimal supported playback rate. See MMAPI docs.
    /// </summary>
    int MinRate { get; }

    /// <summary>
    ///     Gets maximal supported playback rate. See MMAPI docs.
    /// </summary>
    int MaxRate { get; }

    /// <summary>
    ///     Gets current playback rate. See MMAPI docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <returns>Current playback rate. See MMAPI docs.</returns>
    int GetRate(MediaHandle media);

    /// <summary>
    ///     Sets current playback rate. See MMAPI docs.
    /// </summary>
    /// <param name="media">Media handle.</param>
    /// <param name="rate">Playback rate. See MMAPI docs.</param>
    void SetRate(MediaHandle media, int rate);

    #endregion
}