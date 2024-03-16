// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using String = java.lang.String;

namespace javax.microedition.media;

public interface Player : Controllable
{
    public void addPlayerListener([JavaType(typeof(PlayerListener))] Reference listener) => throw new AbstractCall();

    public void close() => throw new AbstractCall();

    public void deallocate() => throw new AbstractCall();

    public String getContentType() => throw new AbstractCall();

    public long getDuration() => throw new AbstractCall();

    public long getMediaTime() => throw new AbstractCall();

    public int getState() => throw new AbstractCall();

    public void prefetch() => throw new AbstractCall();

    public void realize() => throw new AbstractCall();

    public void removePlayerListener([JavaType(typeof(PlayerListener))] Reference listener) => throw new AbstractCall();

    public void setLoopCount(int count) => throw new AbstractCall();

    public long setMediaTime(long time) => throw new AbstractCall();

    public void start() => throw new AbstractCall();

    public void stop() => throw new AbstractCall();
}
