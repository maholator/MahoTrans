// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.util;

public class TimerThread : lang.Thread
{
    public bool Cancelled;

    [JavaType(typeof(TimerTree))] public Reference Tasks;

    [InitMethod]
    public new void Init()
    {
        base.Init();
        var tree = Jvm.AllocateObject<TimerTree>();
        tree.Init();
        Tasks = tree.This;
        start();
    }

    [JavaDescriptor("()V")]
    public new JavaMethodBody run(JavaClass cls)
    {
        // locals: this > task > currentTime > node
        var b = new JavaMethodBuilder(cls);
        var begin = b.PlaceLabel();

        b.AppendThis();
        b.Append(JavaOpcode.monitorenter);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Cancelled), typeof(bool));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendThis();
            b.Append(JavaOpcode.monitorexit);
            b.AppendReturn();
        }

        b.AppendThis();
        b.AppendGetLocalField(nameof(Tasks), typeof(TimerTree));
        b.AppendVirtcall(nameof(TimerTree.isEmpty), typeof(bool));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            using (var tr = b.BeginTry<InterruptedException>())
            {
                b.AppendThis();
                b.AppendVirtcall(nameof(wait), typeof(void));
                tr.CatchSection();
                b.Append(JavaOpcode.pop);
            }

            b.AppendThis();
            b.Append(JavaOpcode.monitorexit);
            b.AppendGoto(JavaOpcode.@goto, begin);
        }

        b.AppendStaticCall<lang.System>(nameof(lang.System.currentTimeMillis), typeof(long));
        b.Append(JavaOpcode.lstore_2);

        // TimerNode taskNode = tasks.minimum();
        // task = taskNode.Task;
        // if (task.Cancelled) tasks.delete(taskNode); continue;
        b.AppendThis();
        b.AppendGetLocalField(nameof(Tasks), typeof(TimerTree));
        b.AppendVirtcall(nameof(TimerTree.minimum), typeof(TimerNode));
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_3);
        b.AppendGetField(nameof(TimerNode.Task), typeof(TimerTask), typeof(TimerNode));
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_1);
        b.AppendGetField(nameof(TimerTask.Cancelled), typeof(bool), typeof(TimerTask));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendThis();
            b.AppendGetLocalField(nameof(Tasks), typeof(TimerTree));
            b.Append(JavaOpcode.aload_3);
            b.AppendVirtcall(nameof(TimerTree.delete), typeof(void), typeof(TimerNode));

            b.AppendThis();
            b.Append(JavaOpcode.monitorexit);
            b.AppendGoto(JavaOpcode.@goto, begin);
        }

        // time check and sleep

        b.Append(JavaOpcode.aload_1);
        b.AppendGetField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));
        b.Append(JavaOpcode.lload_2);
        b.Append(JavaOpcode.lsub);
        b.Append(JavaOpcode.dup2);
        b.Append(JavaOpcode.lstore_2);
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lcmp);
        using (b.AppendGoto(JavaOpcode.ifle))
        {
            using (var tr = b.BeginTry<InterruptedException>())
            {
                b.AppendThis();
                b.Append(JavaOpcode.lload_2);
                b.AppendVirtcall(nameof(wait), typeof(void), typeof(long));
                tr.CatchSection();
                b.Append(JavaOpcode.pop);
            }

            b.AppendThis();
            b.Append(JavaOpcode.monitorexit);
            b.AppendGoto(JavaOpcode.@goto, begin);
        }

        // task.scheduledTime = task.when;
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.aload_1);
        b.AppendGetField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));
        b.AppendPutField(nameof(TimerTask.scheduledTime), typeof(long), typeof(TimerTask));

        // tasks.delete(taskNode);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Tasks), typeof(TimerTree));
        b.Append(JavaOpcode.aload_3);
        b.AppendVirtcall(nameof(TimerTree.delete), typeof(void), typeof(TimerNode));

        b.Append(JavaOpcode.aload_1);
        b.AppendGetField(nameof(TimerTask.period), typeof(long), typeof(TimerTask));
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lcmp);
        using (b.AppendGoto(JavaOpcode.iflt))
        {
            b.Append(JavaOpcode.aload_1);
            b.AppendGetField(nameof(TimerTask.fixedRate), typeof(bool), typeof(TimerTask));
            var l = b.AppendGoto(JavaOpcode.ifne);

            // task.when = task.when + task.period;
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.aload_1);
            b.AppendGetField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));
            b.Append(JavaOpcode.aload_1);
            b.AppendGetField(nameof(TimerTask.period), typeof(long), typeof(TimerTask));
            b.Append(JavaOpcode.ladd);
            b.AppendPutField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));


            var l2 = b.AppendGoto();
            b.BringLabel(l);

            // task.when = java.lang.System.currentTimeMillis() + task.period;

            b.Append(JavaOpcode.aload_1);
            b.AppendStaticCall<lang.System>(nameof(lang.System.currentTimeMillis), typeof(long));
            b.Append(JavaOpcode.aload_1);
            b.AppendGetField(nameof(TimerTask.period), typeof(long), typeof(TimerTask));
            b.Append(JavaOpcode.ladd);
            b.AppendPutField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));

            b.BringLabel(l2);

            // insertTask(task);
            b.AppendThis();
            b.Append(JavaOpcode.aload_1);
            b.AppendVirtcall(nameof(insertTask), typeof(void), typeof(TimerTask));
        }

        b.Append(JavaOpcode.aload_1);
        b.AppendGetField(nameof(TimerTask.period), typeof(long), typeof(TimerTask));
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lcmp);
        using (b.AppendGoto(JavaOpcode.ifge))
        {
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.lconst_0);
            b.AppendPutField(nameof(TimerTask.when), typeof(long), typeof(TimerTask));
        }

        b.AppendThis();
        b.Append(JavaOpcode.monitorexit);

        using (var tr = b.BeginTry<InterruptedException>())
        {
            b.Append(JavaOpcode.aload_1);
            b.AppendVirtcall(nameof(TimerTask.run), typeof(void));
            tr.CatchSection();
            b.Append(JavaOpcode.pop);
        }

        b.AppendGoto(JavaOpcode.@goto, begin);
        return b.Build(3, 4);
    }

    public void insertTask([JavaType(typeof(TimerTask))] Reference newTask)
    {
        var node = Jvm.AllocateObject<TimerNode>();
        node.Init(newTask);
        Tasks.As<TimerTree>().insert(node);
        notify();
    }

    public void cancel()
    {
        Cancelled = true;
        Tasks = Jvm.AllocateObject<TimerTree>().This;
        notify();
    }
}