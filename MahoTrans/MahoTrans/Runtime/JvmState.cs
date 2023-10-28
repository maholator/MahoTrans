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

    public JvmState(Toolkit toolkit)
    {
        Toolkit = toolkit;
    }

    public void RunInContext(Action action)
    {
        if (Object.HeapAttached)
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
        if (Object.HeapAttached)
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
            do
            {
                var ticksAtBegin = DateTime.UtcNow.Ticks;

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

                // attaching timeouted threads
                CheckTimeouts();

                // this will be positive if we are running faster than needed
                var target = Toolkit.Clock.GetTicksPerCycleBunch();
                while (target - (DateTime.UtcNow.Ticks - ticksAtBegin) > 0)
                {
                    Thread.SpinWait(50);
                }
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
}