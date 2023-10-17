using MahoTrans.Native;
using Object = java.lang.Object;

namespace java.util;

public class Date : Object
{
    private long _time;

    [InitMethod]
    public void Init(long ms)
    {
        _time = ms;
    }

    public long getTime() => _time;

    public void setTime(long time) => _time = time;
}