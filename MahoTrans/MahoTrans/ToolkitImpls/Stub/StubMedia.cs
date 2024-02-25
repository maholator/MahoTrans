// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Handles;
using MahoTrans.Runtime;

namespace MahoTrans.ToolkitImpls.Stub;

public class StubMedia : IMedia
{
    public void PlayTone(int note, int duration, int volume)
    {
    }

    public MediaHandle Create(ReadOnlySpan<sbyte> data, string? contentType, Reference callbackTarget)
    {
        return new MediaHandle(1);
    }

    public MediaHandle Create(string mrl, Reference callbackTarget)
    {
        return new MediaHandle(1);
    }

    public string[] GetSupportedContentTypes(string protocol)
    {
        return Array.Empty<string>();
    }

    public string[] GetSupportedProtocols(string contentType)
    {
        return Array.Empty<string>();
    }

    public void Prefetch(MediaHandle media)
    {
    }

    public void Dispose(MediaHandle media)
    {
    }

    public string GetContentType(MediaHandle media)
    {
        return string.Empty;
    }

    public void Start(MediaHandle media)
    {
    }

    public void Stop(MediaHandle media)
    {
    }

    public long? GetDuration(MediaHandle media)
    {
        return null;
    }

    public long? GetTime(MediaHandle media)
    {
        return null;
    }

    public long SetTime(MediaHandle media, long time)
    {
        return time;
    }

    public void SetLoopCount(MediaHandle media, int count)
    {
    }

    public MediaControls GetAvailableControls(MediaHandle media)
    {
        return default;
    }

    public int GetVolume(MediaHandle media)
    {
        return 100;
    }

    public void SetVolume(MediaHandle media, int volume)
    {
    }

    public bool GetMute(MediaHandle media)
    {
        return false;
    }

    public void SetMute(MediaHandle media, bool mute)
    {
    }

    public long GetStopTime(MediaHandle media)
    {
        return -1;
    }

    public void SetStopTime(MediaHandle media, long time)
    {
    }

    public int GetRate(MediaHandle media)
    {
        return 100000;
    }

    public void SetRate(MediaHandle media, int rate)
    {
    }

    public int MinRate => 100000;
    public int MaxRate => 200000;
}