// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using Object = java.lang.Object;

namespace java.util;

public class TimerTask : Object, Runnable
{
    public bool Cancelled;

    public long scheduledTime;

    public long when;
    public long period;
    public bool fixedRate;

    public bool cancel()
    {
        bool willRun = !Cancelled && when > 0;
        Cancelled = true;
        return willRun;
    }

    public void run()
    {
        throw new AbstractCall();
    }

    public long scheduledExecutionTime()
    {
        return scheduledTime;
    }

    public bool isScheduled()
    {
        return when > 0 || scheduledTime > 0;
    }
}