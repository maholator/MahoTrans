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
    /// Runs all registered threads in cycle. This method may never return.
    /// </summary>
    public void Execute()
    {
        RunInContext(() =>
        {
            _running = true;
            var count = AliveThreads.Count;
            while (_running && count > 0)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    var thread = AliveThreads[i];
                    if (thread == null!)
                    {
                        Console.WriteLine(
                            $"Null thread in the list at {i}! Cached count is {count}, real is {AliveThreads.Count}");
                        continue;
                    }

                    if (thread.ActiveFrame == null)
                        AliveThreads.RemoveAt(i);
                    else
                        JavaRunner.Step(thread, this);
                }

                CheckTimeouts();
                count = AliveThreads.Count;
            }
        });
    }

    /// <summary>
    /// Attempts to interrupt this jvm, if it was launched using <see cref="Execute"/>. Returns instantly. Jvm may stop its work after some time.
    /// </summary>
    public void Stop()
    {
        _running = false;
    }

    /// <summary>
    /// Same as <see cref="Execute"/>, but only one operation for each thread. Use to go step-by-step.
    /// </summary>
    public void SpinOnce()
    {
        RunInContext(() =>
        {
            var count = AliveThreads.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var thread = AliveThreads[i];
                if (thread == null!)
                {
                    Console.WriteLine(
                        $"Null thread in the list at {i}! Cached count is {count}, real is {AliveThreads.Count}");
                    continue;
                }

                if (thread.ActiveFrame == null)
                    AliveThreads.RemoveAt(i);
                else
                    JavaRunner.Step(thread, this);
            }

            CheckTimeouts();
        });
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