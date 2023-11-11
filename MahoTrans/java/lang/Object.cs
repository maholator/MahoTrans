using System.Diagnostics;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.lang;

public class Object
{
    #region Object properties

    /// <summary>
    /// Address of this object in attached heap. The following is always true: <code>this.Jvm.ResolveObject(this.HeapAddress) == this</code>
    /// </summary>
    [JavaIgnore] [JsonProperty] public int HeapAddress;

    /// <summary>
    /// Reference to java class, which this object is instance of.
    /// </summary>
    [JavaIgnore] [JsonIgnore] public JavaClass JavaClass = null!;

    /// <summary>
    /// <see cref="HeapAddress"/> wrapped in <see cref="Reference"/> struct.
    /// </summary>
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Reference This => new Reference(HeapAddress);

    /// <summary>
    /// Json helper to serialize/deserialize attached class. NEVER touch it. Use <see cref="JavaClass"/> to take object's class.
    /// Deserialization MUST occur withing JVM context.
    /// </summary>
    [JsonProperty]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string ClassName
    {
        get => JavaClass.Name;
        set => JavaClass = Jvm.GetClass(value);
    }

    #endregion

    #region Context

    [JavaIgnore] [ThreadStatic] [JsonIgnore]
    private static JvmState? _jvm;

    [JsonIgnore]
    public static JvmState Jvm
    {
        get
        {
            if (_jvm == null)
                throw new JavaRuntimeError("Heap is not attached to this thread!");
            return _jvm;
        }
    }

    [JsonIgnore] public static bool JvmAttached => _jvm != null;

    [JsonIgnore] protected static Toolkit Toolkit => Jvm.Toolkit;

    [JavaIgnore]
    public static void AttachHeap(JvmState heap) => _jvm = heap;

    [JavaIgnore]
    public static void DetachHeap() => _jvm = null;

    #endregion

    #region Monitors

    [JavaIgnore] [JsonProperty] public int MonitorOwner;

    [JavaIgnore] [JsonProperty] public uint MonitorReEnterCount;

    [JavaIgnore] [JsonProperty] public List<MonitorWait>? Waiters;

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
        var jvm = Jvm;
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

    #endregion

    #region Java members

    [InitMethod]
    public virtual void Init()
    {
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

    //TODO nanoseconds precision

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

        if (Jvm.Attach(mw.MonitorOwner))
            return;

        throw new JavaRuntimeError($"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
    }

    public void notifyAll()
    {
        if (Waiters == null)
            return;

        foreach (var mw in Waiters)
        {
            if (!Jvm.Attach(mw.MonitorOwner))
                throw new JavaRuntimeError(
                    $"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
        }

        Waiters.Clear();
    }

    [return: JavaType(typeof(String))]
    public virtual Reference toString()
    {
        //TODO
        return Jvm.AllocateString($"Object {JavaClass} @ {GetHashCode()}");
    }

    public bool equals(Reference r)
    {
        return r == This;
    }

    [return: JavaType(typeof(Class))]
    public Reference getClass()
    {
        var cls = Jvm.AllocateObject<Class>();
        cls.InternalClass = JavaClass;
        return cls.This;
    }

    public int hashCode()
    {
        return System.identityHashCode(This);
    }

    #endregion

    #region GC

    /// <summary>
    /// This will be false most of the time. When GC starts going through heap, it will set this field to true on objects which will survive in the pending collection. After collection is finished, this will be changed to false again.
    /// </summary>
    [JavaIgnore] [JsonIgnore] [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool Alive;

    /// <summary>
    /// GC will call this method to collect objects which are referenced by this object. Override it if objects are stored in hidden form.
    /// </summary>
    public virtual void AnnounceHiddenReferences(Queue<Reference> queue)
    {
    }

    /// <summary>
    /// GC will call this method right before object deletion from the heap. Return true to make the object survive in this collection.
    /// </summary>
    public virtual bool OnObjectDelete()
    {
        return false;
    }

    #endregion
}