// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using Object = java.lang.Object;

namespace java.util;

public class Date : Object
{
    public long Time;

    [InitMethod]
    public void Init(long ms)
    {
        Time = ms;
    }

    public long getTime() => Time;

    public void setTime(long time) => Time = time;
}