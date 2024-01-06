// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace java.util;

public class Timer : Object
{
    [JavaType(typeof(TimerThread))] public Reference Thread;

    [InitMethod]
    public new void Init()
    {
        base.Init();
        var th = Jvm.AllocateObject<TimerThread>();
        th.Init();
        Thread = th.This;
    }

    public void cancel()
    {
        Thread.As<TimerThread>().cancel();
    }

    /*
    public void schedule([JavaType(typeof(TimerTask))] Reference task, [JavaType(typeof(Date))] Reference whenRef)
    {
        long delay = AsTime(whenRef) - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, -1, false);
    }

    public void schedule([JavaType(typeof(TimerTask))] Reference task, long delay)
    {
        scheduleImpl(task, delay, -1, false);
    }
*/
    [JavaDescriptor("(Ljava/util/TimerTask;JJ)V")]
    public JavaMethodBody schedule___dp(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);

        b.AppendThis();
        b.AppendGetLocalField(nameof(Thread), typeof(TimerThread));
        b.Append(JavaOpcode.monitorenter);
        using (var tr = b.BeginTry<Throwable>())
        {
            b.AppendThis();
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.lload_2);
            b.Append(JavaOpcode.lload_3);
            b.Append(JavaOpcode.iconst_0);
            b.AppendVirtcall(nameof(scheduleImpl), typeof(void), typeof(Reference), typeof(long), typeof(long), typeof(bool));

            b.AppendThis();
            b.AppendGetLocalField(nameof(Thread), typeof(TimerThread));
            b.Append(JavaOpcode.monitorexit);

            b.Append(JavaOpcode.@return);

            tr.CatchSection();

            b.AppendThis();
            b.AppendGetLocalField(nameof(Thread), typeof(TimerThread));
            b.Append(JavaOpcode.monitorexit);

            b.Append(JavaOpcode.athrow);
        }

        b.Append(JavaOpcode.@return);

        return b.Build(5, 4);
    }

    /*
    public void schedule([JavaType(typeof(TimerTask))] Reference task, [JavaType(typeof(Date))] Reference whenRef,
        long period)
    {
        long delay = AsTime(whenRef) - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, period, false);
    }

    public void scheduleAtFixedRate([JavaType(typeof(TimerTask))] Reference task, long delay, long period)
    {
        scheduleImpl(task, delay, period, true);
    }

    public void scheduleAtFixedRate([JavaType(typeof(TimerTask))] Reference task,
        [JavaType(typeof(Date))] Reference whenRef, long period)
    {
        long delay = AsTime(whenRef) - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, period, true);
    }
    */

    /// <summary>
    /// Takes date's time. Throws it time is less than zero.
    /// </summary>
    /// <param name="date">Date to unwrap.</param>
    /// <returns>JVM ticks.</returns>
    public long AsTime([JavaType(typeof(Date))] Reference date)
    {
        var time = date.As<Date>().getTime();
        if (time < 0) Jvm.Throw<IllegalArgumentException>();

        return time;
    }

    public void scheduleImpl(Reference taskRef, long delay, long period, bool fixedTask)
    {
        if (delay < 0)
            Jvm.Throw<IllegalArgumentException>();

        if (period < -1)
            period = -1;

        TimerTask task = taskRef.As<TimerTask>();
        if (Thread.As<TimerThread>().Cancelled)
            Jvm.Throw<IllegalStateException>();

        long when = delay + java.lang.System.currentTimeMillis();

        if (when < 0)
            Jvm.Throw<IllegalArgumentException>();

        if (task.isScheduled())
            Jvm.Throw<IllegalStateException>();

        if (task.Cancelled)
            Jvm.Throw<IllegalStateException>();

        task.when = when;
        task.period = period;
        task.fixedRate = fixedTask;

        Thread.As<TimerThread>().insertTask(task.This);
    }
}