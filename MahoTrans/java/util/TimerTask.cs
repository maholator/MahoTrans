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