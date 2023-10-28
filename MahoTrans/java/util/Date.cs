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