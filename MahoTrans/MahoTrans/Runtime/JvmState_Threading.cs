namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    /// List of all threads, attached to scheduler. Anyone can append threads here at any time.
    /// Threads must be removed only by themselves during execution, else behaviour is undefined.
    /// </summary>
    public readonly List<JavaThread> AliveThreads = new(256);

    /// <summary>
    /// Threads which are detached from scheduler. For example, waiting for object notify or timeout.
    /// </summary>
    public readonly Dictionary<int, JavaThread> WaitingThreads = new();

    private List<ThreadWakeupHook> _wakeupHooks = new();
    private readonly object _threadPoolLock = new();

    #region Threads management

    /// <summary>
    /// Registers a thread in this JVM.
    /// </summary>
    /// <param name="thread">Thread, ready to run.</param>
    public void RegisterThread(JavaThread? thread)
    {
        if (thread is null)
            throw new NullReferenceException("Attempt to register null thread.");

        Console.WriteLine("Thread registered!");
        lock (_threadPoolLock)
        {
            AliveThreads.Insert(AliveThreads.Count, thread);
        }
    }

    public void Detach(JavaThread thread, long returnAfter)
    {
        lock (_threadPoolLock)
        {
            if (!AliveThreads.Remove(thread))
                throw new JavaRuntimeError($"Attempt to detach {thread} which is not attached.");

            WaitingThreads.Add(thread.ThreadId, thread);
            if (returnAfter >= 0)
            {
                _wakeupHooks.Add(new ThreadWakeupHook(Toolkit.Clock.GetCurrentJvmMs(_cycleNumber) + returnAfter,
                    thread.ThreadId));
            }
        }
    }

    /// <summary>
    /// Moves thread from waiting pool to active pool.
    /// </summary>
    /// <param name="id">Thread id to operate on.</param>
    /// <returns>False, if thread was not in waiting pool. Thread state is undefined in such state.</returns>
    public bool Attach(int id)
    {
        lock (_threadPoolLock)
        {
            if (WaitingThreads.Remove(id, out var th))
            {
                for (int i = 0; i < _wakeupHooks.Count; i++)
                {
                    if (_wakeupHooks[i].ThreadId == id)
                    {
                        _wakeupHooks.RemoveAt(i);
                    }
                }

                AliveThreads.Insert(AliveThreads.Count, th);
                return true;
            }

            return false;
        }
    }

    public void CheckTimeouts()
    {
        var now = Toolkit.Clock.GetCurrentJvmMs(_cycleNumber);

        for (int i = _wakeupHooks.Count - 1; i >= 0; i--)
        {
            if (_wakeupHooks[i].WakeupAtMs <= now)
                Attach(_wakeupHooks[i].ThreadId);
        }
    }

    #endregion
}