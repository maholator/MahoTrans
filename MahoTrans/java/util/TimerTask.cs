// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using Object = java.lang.Object;

namespace java.util;

public class TimerTask : Object, Runnable
{
    public bool Cancelled;

    public bool cancel()
    {
        Cancelled = true;
        return true;
    }
}