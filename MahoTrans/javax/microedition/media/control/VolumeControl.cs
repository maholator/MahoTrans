// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;
using MahoTrans.Native;
using Object = java.lang.Object;

namespace javax.microedition.media.control;

public class VolumeControl : Object, Control
{
    [JavaIgnore] public MediaHandle Handle;

    public int setLevel(int level)
    {
        level = Math.Clamp(level, 0, 100);
        Toolkit.Media.SetVolume(Handle, level);
        return level;
    }
}