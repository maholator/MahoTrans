// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using MahoTrans;
using MahoTrans.Builder;
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
    ///     Address of this object in attached heap. The following is always true:
    ///     <code>this.Jvm.ResolveObject(this.HeapAddress) == this</code>
    /// </summary>
    [JavaIgnore] [JsonProperty] public int HeapAddress;

    /// <summary>
    ///     Reference to java class, which this object is instance of.
    /// </summary>
    [JavaIgnore] [JsonIgnore] public JavaClass JavaClass = null!;

    /// <summary>
    ///     <see cref="HeapAddress" /> wrapped in <see cref="Reference" /> struct.
    /// </summary>
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Reference This => new Reference(HeapAddress);

    /// <summary>
    ///     Json helper to serialize/deserialize attached class. NEVER touch it. Use <see cref="JavaClass" /> to take object's
    ///     class.
    ///     Deserialization MUST occur withing JVM context.
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
            Debug.Assert(_jvm != null, "Heap is not attached to this thread!");
            return _jvm;
        }
    }

    /// <summary>
    ///     Direct access to context slot. Use <see cref="Jvm" /> instead.
    /// </summary>
    [JsonIgnore]
    public static JvmState? JvmUnchecked
    {
        get => _jvm;
        set => _jvm = value;
    }

    [JsonIgnore] public static bool JvmAttached => _jvm != null;

    [JsonIgnore] protected static Toolkit Toolkit => Jvm.Toolkit;

    #endregion

    #region Monitors

    [JavaIgnore] [JsonProperty] public int MonitorOwner;

    [JavaIgnore] [JsonProperty] public uint MonitorReEnterCount;

    [JavaIgnore] [JsonProperty] public List<MonitorWait>? Waiters;

    /// <summary>
    ///     For internal usage. Called by wait() to detach from monitor and scheduler.
    /// </summary>
    /// <returns>Waiter object to store on stack.</returns>
    public long WaitMonitor(long timeout)
    {
        // pending interrupt?
        Jvm.Resolve<Thread>(Thread.currentThread()).CheckInterrupt();

        if (timeout < 0)
            Jvm.Throw<IllegalArgumentException>();

        // adding self to waitlist
        var mw = new MonitorWait(MonitorReEnterCount, MonitorOwner);
        Waiters ??= new();
        Waiters.Add(mw);

        // detaching from scheduler
        var jvm = Jvm;
        var thread = jvm.AliveThreads.Find(x => x.ThreadId == MonitorOwner);
        jvm.Detach(thread!, timeout);

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

        // pending interrupt?
        Jvm.Resolve<Thread>(Thread.currentThread()).CheckInterrupt();
    }

    #endregion

    #region Java members

    [InitMethod]
    public virtual void Init()
    {
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody wait___zero(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.lconst_0);
        b.AppendVirtcall(nameof(wait), typeof(void), typeof(long));
        b.AppendReturn();

        return b.Build(2, 1);
    }

    //TODO nanoseconds precision

    [JavaDescriptor("(J)V")]
    public JavaMethodBody wait(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 2,
            StackSize = 3,
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("WaitMonitor", "(J)J", "java/lang/Object")).Split()),
                // at this point thread is detached

                // running further? seems we have notified.
                // stack: obj > wait_cache
                new(JavaOpcode.aload_0),
                // stack: obj > wait_cache > obj
                new(JavaOpcode.monitorenter),
                // stack: obj > wait_cache
                new(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("FixMonitorAfterWait", "(J)V", "java/lang/Object"))
                        .Split()),
                // at this point monitor state is restored and thread is attached.
                new(JavaOpcode.@return),
            }
        };
    }

    public void notify()
    {
        if (Waiters == null || Waiters.Count == 0)
            return;

        var mw = Waiters[^1];
        Waiters.RemoveAt(Waiters.Count - 1);

        if (Jvm.Attach(mw.MonitorOwner))
            return;

        throw new JavaRuntimeError($"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
    }

    public void notifyAll()
    {
        if (Waiters == null || Waiters.Count == 0)
            return;

        foreach (var mw in Waiters)
        {
            if (!Jvm.Attach(mw.MonitorOwner))
                throw new JavaRuntimeError(
                    $"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
        }

        Waiters.Clear();
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody toString(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendNewObject<StringBuffer>();
        b.Append(JavaOpcode.dup);
        b.AppendVirtcall("<init>", "()V");
        b.AppendThis();
        b.AppendVirtcall(nameof(getClass), "()Ljava/lang/Class;");
        b.AppendVirtcall(nameof(Class.getName), "()Ljava/lang/String;");
        b.AppendVirtcall(nameof(StringBuffer.append), "(Ljava/lang/String;)Ljava/lang/StringBuffer;");
        b.AppendConstant('@');
        b.AppendVirtcall(nameof(StringBuffer.append), "(C)Ljava/lang/StringBuffer;");
        b.AppendThis();
        b.AppendVirtcall(nameof(hashCode), typeof(int));
        b.AppendStaticCall<Integer>(nameof(Integer.toHexString), typeof(String), typeof(int));
        b.AppendVirtcall(nameof(StringBuffer.append), "(Ljava/lang/String;)Ljava/lang/StringBuffer;");
        b.AppendVirtcall(nameof(StringBuffer.toString), "()Ljava/lang/String;");
        b.AppendReturnReference();
        b.AppendReturn();
        return b.Build(2, 1);
    }

    public bool equals(Reference r)
    {
        return r == This;
    }

    [return: JavaType(typeof(Class))]
    public Reference getClass() => JavaClass.GetOrInitModel();

    public int hashCode()
    {
        return System.identityHashCode(This);
    }

    #endregion

    #region GC

    /// <summary>
    ///     This will be false most of the time. When GC starts going through heap, it will set this field to true on objects
    ///     which will survive in the pending collection. After collection is finished, this will be changed to false again.
    /// </summary>
    [JavaIgnore] [JsonIgnore] [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public bool Alive;

    /// <summary>
    ///     GC will call this method to collect objects which are referenced by this object. Override it if objects are stored
    ///     in hidden form.
    /// </summary>
    public virtual void AnnounceHiddenReferences(Queue<Reference> queue)
    {
    }

    /// <summary>
    ///     GC will call this method right before object deletion from the heap. Return true to make the object survive in this
    ///     collection.
    /// </summary>
    public virtual bool OnObjectDelete()
    {
        return false;
    }

    #endregion
}