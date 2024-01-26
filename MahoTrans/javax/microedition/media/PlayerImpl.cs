// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using javax.microedition.media.control;
using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace javax.microedition.media;

public class PlayerImpl : Object, Player
{
    [JavaIgnore] public MediaHandle Handle;

    public int State = UNREALIZED;
    private bool inited;

    /// <summary>
    /// This is >0 if there are working listener threads
    /// </summary>
    public int listenersPending;

    public void realize()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        if (State == UNREALIZED)
            State = REALIZED;
    }

    public void prefetch()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        // implicit realize
        if (State == UNREALIZED)
            State = REALIZED;

        if (State == REALIZED)
        {
            State = PREFETCHED;

            if (!inited)
            {
                Toolkit.Media.Prefetch(Handle);
                inited = true;
            }
        }
    }

    public void start()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        if (State == STARTED)
            return;

        if (State != PREFETCHED)
            prefetch();

        Toolkit.Media.Start(Handle);
        State = STARTED;
    }

    public void stop()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        if (State == STARTED)
        {
            Toolkit.Media.Stop(Handle);
            State = PREFETCHED;
        }
    }

    public void deallocate()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        if (State == STARTED)
            Toolkit.Media.Stop(Handle);
        if (State == UNREALIZED || State == REALIZED)
            return;

        State = REALIZED;
        Toolkit.Media.SetTime(Handle, 0L);
    }

    public void close()
    {
        if (State == CLOSED)
            return;
        if (State == STARTED)
            stop();
        State = CLOSED;
        Toolkit.Media.Dispose(Handle);
        State = CLOSED;
    }

    public void setLoopCount(int count)
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        Toolkit.Media.SetLoopCount(Handle, count);
    }

    public long setMediaTime(long now)
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        if (State == UNREALIZED)
            Jvm.Throw<IllegalStateException>();
        return Toolkit.Media.SetTime(Handle, now);
    }

    public long getMediaTime()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        if (State == UNREALIZED)
            return TIME_UNKNOWN;
        return Toolkit.Media.GetTime(Handle) ?? TIME_UNKNOWN;
    }

    public long getDuration()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        if (State == UNREALIZED)
            return TIME_UNKNOWN;
        return Toolkit.Media.GetDuration(Handle) ?? TIME_UNKNOWN;
    }

    public int getState() => State;

    #region Listeners

    public List<Reference> Listeners = new();

    public void addPlayerListener([JavaType(typeof(PlayerListener))] Reference playerListener)
    {
        if (playerListener.IsNull)
            return;

        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        if (!Listeners.Contains(playerListener))
            Listeners.Add(playerListener);
    }

    public void removePlayerListener([JavaType(typeof(PlayerListener))] Reference playerListener)
    {
        if (playerListener.IsNull)
            return;

        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();

        Listeners.Remove(playerListener);
    }

    /// <summary>
    /// Updates player state based on events. Sends events to listeners. This must be called in context.
    /// </summary>
    /// <param name="eventName">Event name.</param>
    /// <param name="data">Event data. See MIDP docs.</param>
    /// <param name="changeState">If false, <see cref="State"/> won't be touched, only <see cref="Listeners"/> will be notified.</param>
    [JavaIgnore]
    public void Update(string eventName, Reference data, bool changeState)
    {
        if (changeState)
        {
            if (eventName == PlayerListener.STARTED)
            {
                State = STARTED;
            }
            else if (eventName == PlayerListener.END_OF_MEDIA || eventName == PlayerListener.STOPPED)
            {
                if (State == STARTED)
                    State = PREFETCHED;
            }
        }

        if (Listeners.Count == 0)
            return;

        var targets = Jvm.AllocateArray(Listeners.ToArray(), "[Ljavax/microedition/media/PlayerListener;");

        var r = Jvm.AllocateObject<PlayerCallbacksRunnable>();
        r.Init(This, Jvm.InternalizeString(eventName), data, targets);

        var t = Jvm.AllocateObject<Thread>();
        t.InitTargeted(r.This);

        lock (this)
        {
            listenersPending++;
        }

        t.start();
    }

    public void ListenersThreadExited()
    {
        lock (this)
        {
            listenersPending--;
        }
    }

    #endregion

    #region Controls

    [return: JavaType("[Ljavax/microedition/media/Control;")]
    public Reference getControls()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        var volume = AllocVolomeControl();
        return Jvm.AllocateArray(new[] { volume.This }, "[Ljavax/microedition/media/Control;");
    }

    [return: JavaType(typeof(Control))]
    public Reference getControl([String] Reference type)
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        //TODO
        var name = Jvm.ResolveString(type);
        if (name == "javax.microedition.media.control.VolumeControl" || name == "VolumeControl")
        {
            return AllocVolomeControl().This;
        }

        return Reference.Null;
    }

    [JavaIgnore]
    private VolumeControl AllocVolomeControl()
    {
        var ctrl = Jvm.AllocateObject<VolumeControl>();
        ctrl.Handle = Handle;
        ctrl.Player = This;
        return ctrl;
    }

    #endregion

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(Listeners);
    }

    public override bool OnObjectDelete()
    {
        if (State == STARTED)
        {
            // we are still playing
            return true;
        }

        if (listenersPending > 0)
        {
            // listeners are running in background
            return true;
        }

        if (State != CLOSED)
        {
            Toolkit.Media.Dispose(Handle);
            State = CLOSED;
        }

        return false;
    }

    public const int CLOSED = 0;
    public const int PREFETCHED = 300;
    public const int REALIZED = 200;
    public const int STARTED = 400;
    public const long TIME_UNKNOWN = -1L;
    public const int UNREALIZED = 100;
}