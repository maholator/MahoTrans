using javax.microedition.ams;
using JetBrains.Annotations;
using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

/// <summary>
/// Object which holds all information about JVM - threads, objects, classes, etc.
/// </summary>
public partial class JvmState
{
    public Toolkit Toolkit;
    private readonly ExecutionManner _executionManner;

    private bool _running;

    private EventQueue? _eventQueue;

    public Reference MidletObject;

    private long _cycleNumber;

    [PublicAPI] public long CycleNumber => _cycleNumber;

    public const int CYCLES_PER_BUNCH = 1024;

    public event Action<long>? BetweenBunches;

    public JvmState(Toolkit toolkit, ExecutionManner executionManner)
    {
        Toolkit = toolkit;
        _executionManner = executionManner;
    }

    public void RunInContext(Action action)
    {
        var previous = Object.JvmUnchecked;
        try
        {
            Object.JvmUnchecked = this;
            action();
        }
        finally
        {
            Object.JvmUnchecked = previous;
        }
    }

    /// <summary>
    /// Alias for <see cref="Object"/>.<see cref="Object.Jvm"/>.
    /// </summary>
    public static JvmState Context => Object.Jvm;

    /// <summary>
    /// Runs all registered threads in cycle.
    /// </summary>
    public void Execute()
    {
        RunInContext(() =>
        {
            switch (_executionManner)
            {
                case ExecutionManner.Unlocked:
                    ExecuteInternalUnlocked();
                    break;
                case ExecutionManner.Strict:
                    ExecuteInternalStrict();
                    break;
                case ExecutionManner.Weak:
                    ExecuteInternalWeak();
                    break;
            }
        });
    }

    public void ExecuteLoop()
    {
        _running = true;
        Execute();
    }

    /// <summary>
    /// Attempts to interrupt this jvm, if it was launched using <see cref="Execute"/>. Returns instantly. Jvm may stop its work after some time.
    /// </summary>
    public void Stop()
    {
        _running = false;
    }

    public EventQueue EventQueue
    {
        get
        {
            if (_eventQueue != null)
                return _eventQueue;
            RunInContext(() =>
            {
                _eventQueue = AllocateObject<EventQueue>();
                _eventQueue.OwningJvm = this;
                _eventQueue.start();
                Toolkit.Logger.LogDebug(DebugMessageCategory.Threading,
                    $"Event queue created in thread {_eventQueue.JavaThread.ThreadId}");
            });
            return _eventQueue!;
        }
    }

    public void Dispose()
    {
        foreach (var cls in Classes.Values)
        {
            cls.VirtualTable?.Clear();
            cls.VirtualTable = null;
            cls.StaticAnnouncer = null;
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