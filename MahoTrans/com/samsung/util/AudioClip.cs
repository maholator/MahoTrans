// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace com.samsung.util;

public class AudioClip : Object
{
    //TODO
    [InitMethod]
    public void Init(int type, [JavaType("[B")] Reference buf, int audioOffset, int audioLength)
    {
        //stub
    }

    public void play(int loop, int volume)
    {
        // stub
    }

    public void stop()
    {
    }

    public void pause()
    {
    }

    public void resume()
    {
    }
}