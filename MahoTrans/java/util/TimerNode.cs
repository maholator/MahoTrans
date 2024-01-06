// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class TimerNode : Object
{
    [JavaType(typeof(TimerNode))] public Reference Parent;

    [JavaType(typeof(TimerNode))] public Reference Left;

    [JavaType(typeof(TimerNode))] public Reference Right;

    [JavaType(typeof(TimerTask))] public Reference Task;

    [InitMethod]
    public void Init([JavaType(typeof(TimerTask))] Reference timerTask)
    {
        Task = timerTask;
    }
}