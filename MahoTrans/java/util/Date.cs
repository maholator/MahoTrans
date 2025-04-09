// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

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
