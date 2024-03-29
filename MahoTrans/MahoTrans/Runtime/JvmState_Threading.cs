// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime.Errors;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    ///     List of all threads, attached to scheduler.
    ///     This list must NOT be modified outside of <see cref="Detach" />, <see cref="Kill" /> and
    ///     <see cref="CheckWakeups" /> methods.
    ///     First two must be called ONLY from jvm itself. The last must be called ONLY when interpereter is suspended.
    /// </summary>
    public readonly List<JavaThread> AliveThreads = new(256);

    /// <summary>
    ///     Threads which are detached from scheduler. For example, waiting for object notify or timeout.
    /// </summary>
    public readonly Dictionary<int, JavaThread> WaitingThreads = new();

    /// <summary>
    ///     Additional storage for threads from <see cref="WaitingThreads" /> who want to wakeup after some time.
    /// </summary>
    private List<ThreadWakeupHook> _wakeupHooks = new();

    /// <summary>
    ///     This is used to synchronize thread modifications.
    /// </summary>
    private readonly object _threadPoolLock = new();

    /// <summary>
    ///     Threads which were created/attached and waiting to actually do so.
    /// </summary>
    private Queue<JavaThread> _wakeingUpQueue = new();

    public int WakingUpQueueLength => _wakeingUpQueue.Count;

    #region Threads management

    /// <summary>
    ///     Registers a thread in this JVM.
    /// </summary>
    /// <param name="thread">Thread, ready to run.</param>
    public void RegisterThread(JavaThread? thread)
    {
        if (thread is null)
            throw new NullReferenceException("Attempt to register null thread.");

        Toolkit.Logger?.LogEvent(EventCategory.Threading,
            $"Thread {thread.ThreadId} registered and will start soon");
        lock (_threadPoolLock)
        {
            _wakeingUpQueue.Enqueue(thread);
        }
    }

    /// <summary>
    ///     Moves a thread from active pool to waiting pool.
    /// </summary>
    /// <param name="thread">Thread to move.</param>
    /// <param name="returnAfter">If positive sets up a timeout. If negative or zero, no timeout is set.</param>
    /// <param name="monitor">Object on which monitor the thread will wait for notify. Null if there is no monitor.</param>
    /// <exception cref="JavaRuntimeError">If the thread was no running.</exception>
    public void Detach(JavaThread thread, long returnAfter, Reference monitor)
    {
        lock (_threadPoolLock)
        {
            if (!AliveThreads.Remove(thread))
                throw new JavaRuntimeError($"Attempt to detach {thread} which is not attached.");

            WaitingThreads.Add(thread.ThreadId, thread);
            if (returnAfter > 0)
            {
                _wakeupHooks.Add(new ThreadWakeupHook(Toolkit.Clock.GetCurrentJvmMs(_cycleNumber) + returnAfter,
                    thread.ThreadId, monitor));
            }
            else if (!monitor.IsNull)
            {
                // we still need a hook due to monitor object.
                // for example, if thread stopped on wait() and then was interrupted by someone else,
                // it needs to delete itself from wait list.
                _wakeupHooks.Add(new ThreadWakeupHook(long.MaxValue - 1, thread.ThreadId, monitor));
            }
        }
    }

    /// <summary>
    ///     Moves thread from waiting pool to wakeup queue.
    /// </summary>
    /// <param name="id">Thread id to operate on.</param>
    /// <returns>False, if thread was not in waiting pool. Thread state is undefined in such state.</returns>
    public bool Attach(int id)
    {
        lock (_threadPoolLock)
        {
            if (WaitingThreads.Remove(id, out var th))
            {
                // this 2 loops must be reversed because we delete items and want to enumerate FULL list.
                for (int i = _wakeupHooks.Count - 1; i >= 0; i--)
                {
                    if (_wakeupHooks[i].ThreadId == id)
                    {
                        //TODO monitor object may be collected by GC
                        var monitor = _wakeupHooks[i].MonitorObject;
                        if (!monitor.IsNull)
                        {
                            bool alreadyDeleted = false;
                            var waiters = ResolveObject(monitor).Waiters!;
                            for (int j = waiters.Count - 1; j >= 0; j--)
                            {
                                if (waiters[j].MonitorOwner == id)
                                {
                                    if (alreadyDeleted)
                                        throw new JavaRuntimeError("One thread was waiting same object twice");

                                    waiters.RemoveAt(j);
                                    alreadyDeleted = true;
                                }
                            }
                        }

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
    ///     Removes passed thread from JVM completely. Use this if thread finished its work.
    /// </summary>
    /// <param name="thread">Thread to remove.</param>
    /// <returns>False, if this thread was not in jvm.</returns>
    public bool Kill(JavaThread thread)
    {
        lock (_threadPoolLock)
        {
            var killed = AliveThreads.Remove(thread) || WaitingThreads.Remove(thread.ThreadId);

            if (killed)
            {
                foreach (var id in thread.WaitingForKill)
                    Attach(id);
            }

            thread.WaitingForKill.Clear();

            return killed;
        }
    }

    private void CheckWakeups()
    {
        if (_wakeupHooks.Count != 0)
        {
            var now = Toolkit.Clock.GetCurrentJvmMs(_cycleNumber);

            for (int i = _wakeupHooks.Count - 1; i >= 0; i--)
            {
                if (_wakeupHooks[i].WakeupAtMs <= now)
                    Attach(_wakeupHooks[i].ThreadId);
            }
        }

        if (_wakeingUpQueue.Count == 0)
            return;

        lock (_threadPoolLock)
        {
            while (_wakeingUpQueue.Count > 0)
            {
                AliveThreads.Add(_wakeingUpQueue.Dequeue());
            }
        }
    }

    #endregion
}
