// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using javax.microedition.ams;
using javax.microedition.midlet;
using JetBrains.Annotations;
using MahoTrans.Runtime.Config;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

/// <summary>
///     Object which holds all information about JVM - threads, objects, classes, etc and manages JVM execution.
/// </summary>
[PublicAPI]
public sealed partial class JvmState
{
    /// <summary>
    ///     Classes can't be loaded while this is true.
    /// </summary>
    private bool _locked;

    /// <summary>
    ///     Setting this to false will stop execution loop after current cycle.
    /// </summary>
    private bool _shouldRun;

    /// <summary>
    ///     This is true if execution loop is running.
    /// </summary>
    private bool _running;

    private EventQueue? _eventQueue;

    /// <summary>
    ///     Reference to <see cref="MIDlet" />, executed in this JVM.
    /// </summary>
    public Reference MidletObject;

    private long _cycleNumber;

    /// <summary>
    ///     Number of passed cycles.
    /// </summary>
    [PublicAPI]
    public long CycleNumber => _cycleNumber;

    public const long CYCLES_PER_BUNCH = 1024;

    public event Action<long>? BetweenBunches;

    public JvmState(ToolkitCollection toolkit, ExecutionManner executionManner)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Toolkit = toolkit;
        ExecutionManner = executionManner;
    }

    /// <summary>
    ///     Initializes <see cref="EventQueue" />. No events must be dispatched before call to this.
    /// </summary>
    /// <exception cref="InvalidOperationException">Ateempt to create a second queue.</exception>
    public void InitQueue()
    {
        if (_eventQueue != null)
            throw new InvalidOperationException("Attempt to create a second queue");

        _eventQueue = Allocate<EventQueue>();
        _eventQueue.OwningJvm = this;
        _eventQueue.start();
    }

    /// <summary>
    ///     Runs all registered threads in cycle. Behaviour of this is defined by internal interruption flag. Use
    ///     <see cref="ExecuteLoop" /> to be sure that cycle will run until call to <see cref="Stop" />, or call
    ///     <see cref="Stop" /> right before call to this method to be sure that only one cycle will be executed.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Execute()
    {
        if (!_locked)
            throw new InvalidOperationException("Can't run unlocked JVM.");
        try
        {
            _running = true;

            using (new JvmContext(this))
            {
                switch (ExecutionManner)
                {
                    case ExecutionManner.Unlocked:
                        executeInternalUnlocked();
                        break;
                    case ExecutionManner.Strict:
                        executeInternalStrict();
                        break;
                    case ExecutionManner.Weak:
                        executeInternalWeak();
                        break;
                }
            }
        }
        finally
        {
            _running = false;
        }
    }

    /// <summary>
    ///     Resets internal interruption flag and calls <see cref="Execute" />. This will run JVM right inside itself and won't
    ///     return until JVM is stopped.
    /// </summary>
    public void ExecuteLoop()
    {
        _shouldRun = true;
        Execute();
    }

    /// <summary>
    ///     Attempts to interrupt this jvm, if it was launched using <see cref="Execute" />. Returns instantly. Jvm may stop
    ///     its work after some time.
    /// </summary>
    public void Stop()
    {
        _shouldRun = false;
    }

    /// <summary>
    ///     True if JVM execution loop is running. Note that <see cref="Stop" /> call only schedules execution termination. You
    ///     can check this value to know if JVM is actually running or it finally stopped.
    /// </summary>
    public bool IsRunning => _running;

    /// <summary>
    ///     Event queue, associated with this JVM. Use it to dispatch events to midlet.
    /// </summary>
    public EventQueue EventQueue
    {
        get
        {
            Debug.Assert(_eventQueue != null, "Event queue is not initialized!");
            return _eventQueue;
        }
    }

    /// <summary>
    ///     Releases as many references as possible to minimise memory leaks. This object won't be usable anymore.
    /// </summary>
    public void Dispose()
    {
        foreach (var cls in Classes.Values)
        {
            cls.VirtualTableMap?.Clear();
            cls.VirtualTableMap = null;
            cls.VirtualTable = null;
            cls.Super = null!;
            foreach (var f in cls.Fields.Values)
            {
                f.SetValue = null;
                f.GetValue = null;
                f.NativeField = null!;
            }

            cls.Fields.Clear();
            cls.Fields = null!;
            foreach (var m in cls.Methods.Values)
            {
                m.Dispose();
            }

            cls.Methods.Clear();
            cls.Methods = null!;

            cls.ClrType = null;
            cls.Interfaces = null!;
        }

        Classes.Clear();
        Toolkit = null!;
        _eventQueue = null;
        _internalizedStrings.Clear();
        _heap = Array.Empty<Object>();
        AliveThreads.Clear();
        WaitingThreads.Clear();
        _wakeingUpQueue.Clear();
        _wakeupHooks.Clear();
    }
}
