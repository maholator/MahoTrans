// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
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

    public void schedule([JavaType(typeof(TimerTask))] Reference task, [JavaType(typeof(Date))] Reference whenRef)
    {
        var when = whenRef.As<Date>();
        if (when.getTime() < 0)
            Jvm.Throw<IllegalArgumentException>();
        long delay = when.getTime() - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, -1, false);
    }

    public void schedule([JavaType(typeof(TimerTask))] Reference task, long delay)
    {
        if (delay < 0)
            Jvm.Throw<IllegalArgumentException>();
        scheduleImpl(task, delay, -1, false);
    }

    public void schedule([JavaType(typeof(TimerTask))] Reference task, long delay, long period)
    {
        if (delay < 0 || period <= 0)
            Jvm.Throw<IllegalArgumentException>();
        scheduleImpl(task, delay, period, false);
    }

    public void schedule([JavaType(typeof(TimerTask))] Reference task, [JavaType(typeof(Date))] Reference whenRef,
        long period)
    {
        var when = whenRef.As<Date>();
        if (period <= 0 || when.getTime() < 0)
        {
            Jvm.Throw<IllegalArgumentException>();
        }

        long delay = when.getTime() - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, period, false);
    }

    public void scheduleAtFixedRate([JavaType(typeof(TimerTask))] Reference task, long delay, long period)
    {
        if (delay < 0 || period <= 0)
            Jvm.Throw<IllegalArgumentException>();
        scheduleImpl(task, delay, period, true);
    }

    public void scheduleAtFixedRate([JavaType(typeof(TimerTask))] Reference task,
        [JavaType(typeof(Date))] Reference whenRef, long period)
    {
        var when = whenRef.As<Date>();
        if (period <= 0 || when.getTime() < 0)
            Jvm.Throw<IllegalArgumentException>();
        long delay = when.getTime() - java.lang.System.currentTimeMillis();
        scheduleImpl(task, delay < 0 ? 0 : delay, period, true);
    }

    private void scheduleImpl(Reference taskRef, long delay, long period, bool fixedTask)
    {
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

        // TODO this must be synchronized somehow
        Thread.As<TimerThread>().insertTask(task.This);
    }
}