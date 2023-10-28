namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    /// List of all threads, attached to scheduler.
    /// This list must NOT be modified outside of <see cref="Detach"/>, <see cref="Kill"/> and <see cref="CheckWakeups"/> methods.
    /// First two must be called ONLY from jvm itself. The lust must be called ONLY when interpereter is suspended.
    /// </summary>
    public readonly List<JavaThread> AliveThreads = new(256);

    /// <summary>
    /// Threads which are detached from scheduler. For example, waiting for object notify or timeout.
    /// </summary>
    public readonly Dictionary<int, JavaThread> WaitingThreads = new();

    /// <summary>
    /// Additional storage for threads from <see cref="WaitingThreads"/> who want to wakeup after some time.
    /// </summary>
    private List<ThreadWakeupHook> _wakeupHooks = new();

    /// <summary>
    /// This is used to synchronize thread modifications.
    /// </summary>
    private readonly object _threadPoolLock = new();

    /// <summary>
    /// Threads which we created/attached and waiting to actually do so.
    /// </summary>
    private Queue<JavaThread> _wakeingUpQueue = new();

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
            _wakeingUpQueue.Enqueue(thread);
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

                _wakeingUpQueue.Enqueue(th);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Removes passed thread from JVM completely. Use this if thread finished its work.
    /// </summary>
    /// <param name="thread">Thread to remove.</param>
    /// <returns>False, if this thread was not in jvm.</returns>
    public bool Kill(JavaThread thread)
    {
        lock (_threadPoolLock)
        {
            return AliveThreads.Remove(thread) || WaitingThreads.Remove(thread.ThreadId);
        }
    }

    public void CheckWakeups()
    {
        var now = Toolkit.Clock.GetCurrentJvmMs(_cycleNumber);

        for (int i = _wakeupHooks.Count - 1; i >= 0; i--)
        {
            if (_wakeupHooks[i].WakeupAtMs <= now)
                Attach(_wakeupHooks[i].ThreadId);
        }

        if (_wakeingUpQueue.Count != 0)
        {
            lock (_threadPoolLock)
            {
                while (_wakeingUpQueue.Count > 0)
                {
                    AliveThreads.Add(_wakeingUpQueue.Dequeue());
                }
            }
        }
    }

    #endregion
}