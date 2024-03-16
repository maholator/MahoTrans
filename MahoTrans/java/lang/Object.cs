// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.lang;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Object : IJavaObject
{
    #region Object properties

    /// <summary>
    ///     Address of this object in attached heap. The following is always true:
    ///     <code>this.Jvm.ResolveObject(this.HeapAddress) == this</code>
    /// </summary>
    [JavaIgnore]
    [JsonProperty]
    public int HeapAddress;

    /// <summary>
    ///     Reference to java class, which this object is instance of.
    /// </summary>
    [JavaIgnore]
    [JsonIgnore]
    public JavaClass JavaClass = null!;

    /// <summary>
    ///     <see cref="HeapAddress" /> wrapped in <see cref="Reference" /> struct.
    /// </summary>
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Reference This
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => new(HeapAddress);
    }

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

    [JsonIgnore]
    public static JvmState Jvm
    {
        get
        {
            var jvm = JvmContext.Jvm;
            Debug.Assert(jvm != null, "Jvm is not attached to this thread!");
            return jvm;
        }
    }

    [JsonIgnore]
    protected static ToolkitCollection Toolkit => JvmContext.Toolkit!;

    [JsonIgnore]
    public static StaticMemory NativeStatics => Jvm.StaticMemory;

    #endregion

    #region Monitors

    [JavaIgnore]
    [JsonProperty]
    private int _monitorOwner;

    [JavaIgnore]
    [JsonProperty]
    private uint _monitorReEnterCount;

    [JavaIgnore]
    [JsonProperty]
    public List<MonitorWait>? Waiters;

    /// <summary>
    ///     For internal usage. Called by wait() to detach from monitor and scheduler.
    /// </summary>
    /// <returns>Waiter object to store on stack.</returns>
    public long WaitMonitor(long timeout)
    {
        // pending interrupt? throwing right now.
        Jvm.Resolve<Thread>(Thread.currentThread()).CheckInterrupt();

        // cldc check
        if (timeout < 0)
            Jvm.Throw<IllegalArgumentException>();

        // thread MUST own the monitor as per docs.
        var thrId = Thread.CurrentThread!.ThreadId;
        if (_monitorOwner == 0)
            Jvm.Throw<IllegalMonitorStateException>(
                $"Monitor {HeapAddress} was not locked to anybody. Wait() was invoked from thread {thrId}.");
        if (_monitorOwner != thrId)
            Jvm.Throw<IllegalMonitorStateException>(
                $"Monitor {HeapAddress} was locked by {_monitorOwner}, {thrId} attempts to wait().");

        // we must not wait on objects which not in heap.
        if (HeapAddress == 0)
            throw new JavaRuntimeError("Can't wait on object with zero address.");

        // adding self to wait list at this object
        var mw = new MonitorWait(_monitorReEnterCount, _monitorOwner);
        Waiters ??= new();
        Waiters.Add(mw);

        // detaching from scheduler
        var jvm = Jvm;
        jvm.Detach(Thread.CurrentThread, timeout, This);

        // leaving the monitor (so other thread can enter this monitor and call notify())
        _monitorOwner = 0;
        _monitorReEnterCount = 0;

        return mw; //TODO force thread yield when running AOT
    }

    public void FixMonitorAfterWait(long mwPacked)
    {
        MonitorWait mw = mwPacked;

        // monitor owner was set by monitorenter opcode.
        // MW was stored on thread stack, so if enter instruction in wait() glue was done correctly, owners must match.
        if (_monitorOwner != mw.MonitorOwner)
            throw new JavaRuntimeError("After wait, thread that owns the object was changed.");

        // fix enter level
        _monitorReEnterCount = mw.MonitorReEnterCount;

        // pending interrupt?
        Jvm.Resolve<Thread>(Thread.currentThread()).CheckInterrupt();
    }

    /// <summary>
    ///     Attempt to lock monitor to specified thread. If the monitor is owned by another thread, this does nothing and
    ///     returns false.
    /// </summary>
    /// <param name="thread">Thread to lock monitor to.</param>
    /// <returns>True if monitor was entered, false if not.</returns>
    /// <remarks>
    ///     This API is for interpreter.
    /// </remarks>
    [JavaIgnore]
    public bool TryEnterMonitor(JavaThread thread)
    {
        if (_monitorOwner == 0)
        {
            Debug.Assert(_monitorReEnterCount == 0, "Free monitor must be entered on zero level");
            _monitorOwner = thread.ThreadId;
            _monitorReEnterCount = 1;
            return true;
        }

        if (_monitorOwner == thread.ThreadId)
        {
            Debug.Assert(_monitorReEnterCount >= 1, "Locked monitor must be entered on at least one level");
            _monitorReEnterCount++;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Exits the monitor. If there is an attempt to exit with thread that did not own the monitor, java exception will be
    ///     thrown.
    /// </summary>
    /// <param name="thread">Thread to exit monitor with.</param>
    /// <remarks>
    ///     This API is for interpreter.
    /// </remarks>
    [JavaIgnore]
    public void ExitMonitor(JavaThread thread)
    {
        if (_monitorOwner != thread.ThreadId)
            Jvm.Throw<IllegalMonitorStateException>();

        Debug.Assert(_monitorReEnterCount >= 1, "Monitor must be entered when exiting");
        _monitorReEnterCount--;
        if (_monitorReEnterCount == 0)
            _monitorOwner = 0;
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
    public JavaMethodBody wait(JavaClass cls)
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
                    cls.PushConstant(new NameDescriptorClass(nameof(WaitMonitor), "(J)J", "java/lang/Object")).Split()),
                // at this point thread is detached

                // running further? seems we have notified.
                // stack: obj > wait_cache
                new(JavaOpcode.aload_0),
                // stack: obj > wait_cache > obj
                // monitorenter is here because we must REENTER it.
                // There could be several thread waiting for it (i.e. notifyAll on 2+), so this goes through retry logic in interpreter.
                new(JavaOpcode.monitorenter),
                // stack: obj > wait_cache
                // "fixer" checks that we really entered the monitor on previous instruction (throws if not) and resets "enter level".
                new(JavaOpcode.invokespecial,
                    cls.PushConstant(new NameDescriptorClass(nameof(FixMonitorAfterWait), "(J)V", "java/lang/Object"))
                        .Split()),
                // at this point monitor state is restored and thread is attached.
                new(JavaOpcode.@return),
            }
        };
    }

    public void notify()
    {
        //TODO check monitor owner per CLDC specs?

        if (Waiters == null || Waiters.Count == 0)
            return;

        var mw = Waiters[^1];

        if (Jvm.Attach(mw.MonitorOwner))
            return;

        // attach logic will delete us from wait list.

        throw new JavaRuntimeError($"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
    }

    public void notifyAll()
    {
        //TODO check monitor owner per CLDC specs?

        if (Waiters == null || Waiters.Count == 0)
            return;

        for (var i = Waiters.Count - 1; i >= 0; i--)
        {
            var mw = Waiters[i];
            if (!Jvm.Attach(mw.MonitorOwner))
                throw new JavaRuntimeError(
                    $"Attempt to notify thread {mw.MonitorOwner}, but it didn't wait for anything.");
        }

        // waiters list must be cleared by attach calls
        if (Waiters.Count != 0)
            throw new JavaRuntimeError($"NotifyAll() left some ({Waiters.Count}) threads still waiting.");
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
        b.AppendChar('@');
        b.AppendVirtcall(nameof(StringBuffer.append), "(C)Ljava/lang/StringBuffer;");
        b.AppendThis();
        b.AppendVirtcall(nameof(hashCode), typeof(int));
        b.AppendStaticCall<Integer>(nameof(Integer.toHexString), typeof(String), typeof(int));
        b.AppendVirtcall(nameof(StringBuffer.append), "(Ljava/lang/String;)Ljava/lang/StringBuffer;");
        b.AppendVirtcall(nameof(StringBuffer.toString), "()Ljava/lang/String;");
        b.AppendReturnReference();
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
    [JavaIgnore]
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
