// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.lang;

public class Thread : Object, Runnable
{
    public const int MAX_PRIORITY = 10;
    public const int MIN_PRIORITY = 1;
    public const int NORM_PRIORITY = 5;
    /// <summary>
    ///     Reference to JVM object of the thread. During wakeup, this is validated by
    ///     <see cref="JvmState.SyncHeapAfterRestore" />. This is null if thread was not started yet or was already dead.
    /// </summary>
    [JavaIgnore] [JsonIgnore] public JavaThread? JavaThread;

    [JavaIgnore] [ThreadStatic] [JsonIgnore]
    public static JavaThread? CurrentThread;

    [JavaType(typeof(Runnable))] public Reference _target;
    [String] public Reference _name;
    public bool started;

    public bool Interrupted;

    [InitMethod]
    public void InitEmpty()
    {
    }

    [InitMethod]
    public void InitNamed([String] Reference str)
    {
        _name = str;
    }

    [InitMethod]
    public void InitTargeted([JavaType(typeof(Runnable))] Reference t)
    {
        _target = t;
    }

    [InitMethod]
    public void InitFull([JavaType(typeof(Runnable))] Reference t, [String] Reference str)
    {
        _target = t;
        _name = str;
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 2,
            Code = new[]
            {
                new Instruction(0, JavaOpcode.aload_0),
                new Instruction(1, JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("_target", "Ljava/lang/Runnable;", "java/lang/Thread"))
                        .Split()),
                new Instruction(4, JavaOpcode.dup),
                new Instruction(5, JavaOpcode.ifnull, new byte[] { 0, 7 }),
                new Instruction(8, JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptorClass("run", "()V", "java/lang/Runnable")).Split()),
                new Instruction(11, JavaOpcode.@return),
                new Instruction(12, JavaOpcode.@return),
            }
        };
    }

    [return: JavaType(typeof(Thread))]
    public static Reference currentThread() => CurrentThread!.Model;

    public void setPriority(int p)
    {
    }

    [JavaDescriptor("(J)V")]
    public static JavaMethodBody sleep(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.Append(JavaOpcode.lload_0);
        b.AppendStaticCall<Thread>(nameof(SleepInternal), typeof(void), typeof(long));
        b.AppendStaticCall<Thread>(nameof(currentThread), typeof(Thread));
        b.AppendVirtcall(nameof(CheckInterrupt), typeof(void));
        b.AppendReturn();
        return b.Build(1, 1);
    }

    public static void SleepInternal(long time)
    {
        var threadToSleep = CurrentThread!;

        Jvm.Resolve<Thread>(threadToSleep.Model).CheckInterrupt();

        Jvm.Detach(threadToSleep, time <= 0 ? 1 : time);
    }

    public void interrupt()
    {
        // setting interrupt flag - nearest sleep/wait will check for it.
        Interrupted = true;
        // if thread was sleeping, wake it up
        if (JavaThread != null)
            Jvm.Attach(JavaThread.ThreadId);
    }

    /// <summary>
    ///     Internal method that checks interruption flag. If set, the flag is reset and this throws.
    /// </summary>
    public void CheckInterrupt()
    {
        if (Interrupted)
        {
            Interrupted = false;
            Jvm.Throw<InterruptedException>();
        }
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody join(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(JoinInternal), typeof(void));
        b.AppendStaticCall<Thread>(nameof(currentThread), typeof(Thread));
        b.AppendVirtcall(nameof(CheckInterrupt), typeof(void));
        b.AppendReturn();
        return b.Build(1, 1);
    }

    public void JoinInternal()
    {
        // this is who waits
        var waiter = CurrentThread!;
        // this is for who we wait
        var waitFor = JavaThread;

        var currentThreadObject = Jvm.Resolve<Thread>(waiter.Model);

        currentThreadObject.CheckInterrupt();

        if (waitFor?.ActiveFrame == null)
        {
            // if thread is already dead, we just return.
            return;
        }

        waitFor.WaitingForKill.Add(waiter.ThreadId);
        Jvm.Detach(waiter, 0);
    }

    public static void yield()
    {
        //when running on interpreter this is a no-op
    }

    public bool isAlive() => JavaThread?.ActiveFrame != null;

    public static int activeCount() => Jvm.AliveThreads.Count + Jvm.WaitingThreads.Count;

    public void start()
    {
        if (started)
            Jvm.Throw<IllegalThreadStateException>();
        started = true;
        var javaThread = JavaThread.CreateReal(this);
        Jvm.RegisterThread(javaThread);
        JavaThread = javaThread;
    }

    [return: String]
    public Reference getName()
    {
        return _name;
    }

    public int getPriority()
    {
        return NORM_PRIORITY;
    }
}