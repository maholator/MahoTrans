using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkit;
using MahoTrans.Utils;

namespace java.lang;

public class Object
{
    [JavaIgnore] public int HeapAddress;

    /// <summary>
    /// Reference to java class, which this object is instance of.
    /// </summary>
    [JavaIgnore] public JavaClass JavaClass = null!;

    [JavaIgnore] [ThreadStatic] private static JavaHeap? _heap;
    [JavaIgnore] public int MonitorOwner;
    [JavaIgnore] public uint MonitorReEnterCount;
    [JavaIgnore] public List<MonitorWait>? Waiters;

    public static JavaHeap Heap
    {
        get
        {
            if (_heap == null)
                throw new JavaRuntimeError("Heap is not attached to this thread!");
            return _heap;
        }
    }

    public static bool HeapAttached => _heap != null;

    protected static IToolkit Toolkit => Heap.State.Toolkit;

    public Reference This => new Reference(HeapAddress);

    [JavaIgnore]
    public static void AttachHeap(JavaHeap heap) => _heap = heap;

    [JavaIgnore]
    public static void DetachHeap() => _heap = null;

    [InitMethod]
    public virtual void Init()
    {
    }

    /// <summary>
    /// For internal usage. Called by <see cref="wait()"/> to detach from monitor and scheduler.
    /// </summary>
    /// <returns>Waiter object to store on stack.</returns>
    public long WaitMonitor(long timeout)
    {
        // adding self to waitlist
        var mw = new MonitorWait(MonitorReEnterCount, MonitorOwner);
        Waiters ??= new();
        Waiters.Add(mw);

        // detaching from scheduler
        var jvm = Heap.State;
        var thread = jvm.AliveThreads.Find(x => x.ThreadId == MonitorOwner);
        jvm.Detach(thread!, timeout <= 0 ? -1 : timeout);

        // leaving the monitor
        MonitorOwner = 0;
        MonitorReEnterCount = 0;

        return mw; //TODO force thread yield when running AOT
    }

    public void FixMonitorAfterWait(long mwPacked)
    {
        MonitorWait mw = mwPacked;
        MonitorReEnterCount = mw.MonitorReEnterCount;
        if (MonitorOwner != mw.MonitorOwner)
            throw new JavaRuntimeError("After wait, thread that owns the object was changed.");
    }

    [JavaDescriptor("()V")]
    public static JavaMethodBody wait___zero(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 0,
            StackSize = 2,
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.lconst_0),
                new Instruction(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("wait", "(J)V", "java/lang/Object")).Split()),
                new Instruction(JavaOpcode.areturn)
            }
        };
    }

    [JavaDescriptor("(J)V")]
    public static JavaMethodBody wait(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 2,
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.aload_1),
                new Instruction(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("WaitMonitor", "(J)J", "java/lang/Object")).Split()),
                // at this point thread is detached

                // running further? seems we have notified.
                // stack: wait_cache
                new Instruction(JavaOpcode.aload_0),
                // stack: wait_cache > obj
                new Instruction(JavaOpcode.monitorenter),
                // stack: wait_cache
                // we are at monitor, but let's fix it
                new Instruction(JavaOpcode.aload_0),
                // stack: wait_cache > obj
                new Instruction(JavaOpcode.swap),
                // stack: obj > wait_cache
                new Instruction(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("FixMonitorAfterWait", "(J)V", "java/lang/Object"))
                        .Split()),
                // at this point monitor state is restored and thread is attached.
                new Instruction(JavaOpcode.@return),
            }
        };
    }

    public void notify()
    {
        if (Waiters == null)
            return;

        var mw = Waiters[^1];
        Waiters.RemoveAt(Waiters.Count - 1);

        if (Heap.State.Attach(mw.MonitorOwner))
            return;

        throw new JavaRuntimeError($"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
    }

    public void notifyAll()
    {
        if (Waiters == null)
            return;

        foreach (var mw in Waiters)
        {
            if (!Heap.State.Attach(mw.MonitorOwner))
                throw new JavaRuntimeError(
                    $"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
        }

        Waiters.Clear();
    }

    [return: JavaType(typeof(String))]
    public virtual Reference toString()
    {
        //TODO
        return Heap.AllocateString($"Object {JavaClass} @ {GetHashCode()}");
    }

    public bool equals(Reference r)
    {
        return r == This;
    }

    [return: JavaType(typeof(Class))]
    public Reference getClass()
    {
        var cls = Heap.AllocateObject<Class>();
        cls.InternalClass = JavaClass;
        return cls.This;
    }
}