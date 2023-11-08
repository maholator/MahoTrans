using javax.microedition.ams;
using MahoTrans.Loader;
using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

/// <summary>
/// Object which holds all information about JVM - threads, objects, classes, etc.
/// </summary>
public partial class JvmState
{
    public Toolkit Toolkit;

    private bool _running;

    private EventQueue? _eventQueue;

    private long _cycleNumber;

    public long CycleNumber => _cycleNumber;

    public const int CYCLES_PER_BUNCH = 1024;

    public event Action<long>? BetweenBunches;

    public JvmState(Toolkit toolkit)
    {
        Toolkit = toolkit;
    }

    public void RunInContext(Action action)
    {
        if (Object.JvmAttached)
            throw new JavaRuntimeError("This thread already has attached context!");

        try
        {
            Object.AttachHeap(this);
            action();
        }
        finally
        {
            Object.DetachHeap();
        }
    }

    public void RunInContextIfNot(Action action)
    {
        if (Object.JvmAttached)
        {
            action();
            return;
        }

        RunInContext(action);
    }

    /// <summary>
    /// Runs all registered threads in cycle.
    /// </summary>
    public void Execute()
    {
        RunInContext(() =>
        {
            long clrTicks = DateTime.UtcNow.Ticks;
            do
            {
                do
                {
                    for (int i = AliveThreads.Count - 1; i >= 0; i--)
                    {
                        JavaRunner.Step(AliveThreads[i], this);
                    }

                    _cycleNumber++;

                    if (_cycleNumber % CYCLES_PER_BUNCH == 0)
                    {
                        break;
                    }
                } while (_running);

                // synchronize with externals
                BetweenBunches?.Invoke(_cycleNumber);

                // attaching threads who want to attach
                CheckWakeups();

                // gc
                if (_gcPending)
                {
                    _gcPending = false;
                    RunGarbageCollector();
                }

                // this will be positive if we are running faster than needed
                var target = Toolkit.Clock.GetTicksPerCycleBunch();
                while (target - (DateTime.UtcNow.Ticks - clrTicks) > 0)
                {
                    Thread.SpinWait(50);
                }

                clrTicks += target;
            } while (_running);
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
            RunInContextIfNot(() =>
            {
                Console.WriteLine("Starting queue processor...");
                _eventQueue = AllocateObject<EventQueue>();
                _eventQueue.OwningJvm = this;
                _eventQueue.start();
            });
            return _eventQueue!;
        }
    }

    public void LogOpcodeStats()
    {
        var d = ClassLoader.CountOpcodes(Classes.Values);

        foreach (var kvp in d.Where(x => x.Value != 0).OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"{kvp.Key.ToString(),-16} {kvp.Value}");
        }

        Console.WriteLine();
        Console.WriteLine("Unused:");

        foreach (var kvp in d.Where(x => x.Value == 0))
        {
            Console.WriteLine($"{kvp.Key}");
        }
    }

    public void Dispose()
    {
        var assemblies = Classes.Values.Select(x => x.ClrType?.Assembly)
            .Where(x => x != null && x.IsDynamic && x.FullName.StartsWith("jar")).Distinct();
        Console.WriteLine($"Assemblies count: {assemblies.Count()}");
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