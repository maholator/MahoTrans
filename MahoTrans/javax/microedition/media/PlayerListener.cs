// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;

namespace javax.microedition.media;

[JavaInterface]
public interface PlayerListener
{
    public const string CLOSED = "closed";
    public const string DEVICE_AVAILABLE = "deviceAvailable";
    public const string DEVICE_UNAVAILABLE = "deviceUnavailable";
    public const string DURATION_UPDATED = "durationUpdated";
    public const string END_OF_MEDIA = "endOfMedia";
    public const string ERROR = "error";
    public const string STARTED = "started";
    public const string STOPPED = "stopped";
    public const string VOLUME_CHANGED = "volumeChanged";
}