// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.media.control;

public class VolumeControl : Object, Control
{
    [JavaIgnore]
    public MediaHandle Handle;

    /// <summary>
    ///     Reference to the player. Used to block GC from collecting it.
    /// </summary>
    public Reference Player;

    public int setLevel(int level)
    {
        level = Math.Clamp(level, 0, 100);
        Toolkit.Media.SetVolume(Handle, level);
        return level;
    }

    public int getLevel() => Toolkit.Media.GetVolume(Handle);

    public void setMute(bool mute)
    {
        Toolkit.Media.SetMute(Handle, mute);
    }

    public bool isMuted() => Toolkit.Media.GetMute(Handle);
}
