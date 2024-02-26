// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using javax.microedition.media.control;
using MahoTrans;
using MahoTrans.Abstractions;
using MahoTrans.Builder;
using MahoTrans.Handles;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;
using String = java.lang.String;
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

    public void checkNotClosed()
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
    }

    public void realize()
    {
        checkNotClosed();
        if (State == UNREALIZED)
            State = REALIZED;
    }

    public void prefetch()
    {
        checkNotClosed();

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

    [JavaDescriptor("()V")]
    public JavaMethodBody start(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(checkNotClosed), typeof(void));
        b.AppendThis();
        b.AppendVirtcall(nameof(getState), typeof(int));
        b.AppendShort(STARTED);
        using (b.AppendGoto(JavaOpcode.if_icmpne))
        {
            b.AppendReturn();
        }

        AppendMediaTimeCallbackConstruction(b, PlayerListener.STARTED);

        b.AppendThis();
        b.AppendVirtcall(nameof(startInternal), typeof(void));

        b.AppendVirtcall(nameof(PlayerCallbacksRunnable.run), typeof(void));
        b.AppendReturn();

        return b.Build(7, 1);
    }

    [JavaIgnore]
    private static void AppendMediaTimeCallbackConstruction(JavaMethodBuilder b, string eventName)
    {
        b.AppendNewObject<PlayerCallbacksRunnable>();
        b.Append(JavaOpcode.dup);

        // runnable > runnable

        b.AppendThis();
        b.AppendConstant(eventName);

        // runnable > runnable > this > event
        b.AppendNewObject<Long>();
        b.Append(JavaOpcode.dup);
        b.AppendThis();
        b.AppendVirtcall(nameof(getMediaTime), typeof(long));
        b.AppendVirtcall("<init>", typeof(void), typeof(long));

        // runnable > runnable > this > event > time

        b.AppendThis();
        b.AppendVirtcall(nameof(getListeners), "()[Ljavax/microedition/media/PlayerListener;");

        // runnable > runnable > this > event > time > listeners

        b.AppendVirtcall("<init>", typeof(void), typeof(Player), typeof(String), typeof(Object), typeof(Object));

        // runnable
    }

    public void startInternal()
    {
        if (State != PREFETCHED)
            prefetch();

        Toolkit.Media.Start(Handle);
        State = STARTED;
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody stop(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(checkNotClosed), typeof(void));
        b.AppendThis();
        b.AppendVirtcall(nameof(getState), typeof(int));
        b.AppendShort(STARTED);
        using (b.AppendGoto(JavaOpcode.if_icmpeq))
        {
            b.AppendReturn();
        }

        AppendMediaTimeCallbackConstruction(b, PlayerListener.STOPPED);

        b.AppendThis();
        b.AppendVirtcall(nameof(stopInternal), typeof(void));

        b.AppendVirtcall(nameof(PlayerCallbacksRunnable.run), typeof(void));
        b.AppendReturn();

        return b.Build(7, 1);
    }

    public void stopInternal()
    {
        Toolkit.Media.Stop(Handle);
        State = PREFETCHED;
    }

    public void deallocate()
    {
        checkNotClosed();

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
            stopInternal();
        Toolkit.Logger?.LogRuntime(MTLogLevel.Info, $"Closing player {Handle.Id} via close() call");
        State = CLOSED;
        Toolkit.Media.Dispose(Handle);
        State = CLOSED;
    }

    public void setLoopCount(int count)
    {
        checkNotClosed();
        Toolkit.Media.SetLoopCount(Handle, count);
    }

    public long setMediaTime(long now)
    {
        checkNotClosed();
        if (State == UNREALIZED)
            Jvm.Throw<IllegalStateException>();
        return Toolkit.Media.SetTime(Handle, now);
    }

    public long getMediaTime()
    {
        checkNotClosed();
        if (State == UNREALIZED)
            return TIME_UNKNOWN;
        return Toolkit.Media.GetTime(Handle) ?? TIME_UNKNOWN;
    }

    public long getDuration()
    {
        checkNotClosed();
        if (State == UNREALIZED)
            return TIME_UNKNOWN;
        return Toolkit.Media.GetDuration(Handle) ?? TIME_UNKNOWN;
    }

    public int getState() => State;

    #region Listeners

    public List<Reference> Listeners = new();

    [return: JavaType("[Ljavax/microedition/media/PlayerListener;")]
    public Reference getListeners()
    {
        return Jvm.WrapReferenceArray(Listeners.ToArray(), "[Ljavax/microedition/media/PlayerListener;");
    }

    public void addPlayerListener([JavaType(typeof(PlayerListener))] Reference playerListener)
    {
        if (playerListener.IsNull)
            return;

        checkNotClosed();

        if (!Listeners.Contains(playerListener))
            Listeners.Add(playerListener);
    }

    public void removePlayerListener([JavaType(typeof(PlayerListener))] Reference playerListener)
    {
        if (playerListener.IsNull)
            return;

        checkNotClosed();

        Listeners.Remove(playerListener);
    }

    /// <summary>
    /// Sends events related to media end. This must be called in context.
    /// </summary>
    /// <param name="looped">True if player was started again.</param>
    [JavaIgnore]
    public void OnMediaEnd(bool looped)
    {
        if (State == CLOSED)
        {
            throw new JavaRuntimeError(
                $"Closed player {Handle.Id} got media end event. Loop: {looped}");
        }

        if (!looped)
        {
            if (State == STARTED)
                State = PREFETCHED;
        }

        if (Listeners.Count == 0)
            return;

        var targets = Jvm.WrapReferenceArray(Listeners.ToArray(), "[Ljavax/microedition/media/PlayerListener;");

        // end
        {
            var l = Jvm.Allocate<Long>();
            l.Init(getDuration());
            var r = Jvm.Allocate<PlayerCallbacksRunnable>();
            r.Init(This, Jvm.InternalizeString(PlayerListener.END_OF_MEDIA), l.This, targets);
            var t = Jvm.Allocate<Thread>();
            t.InitTargeted(r.This);
            lock (this)
                listenersPending++;
            t.start();
        }
        if (looped)
        {
            var l = Jvm.Allocate<Long>();
            l.Init(0L);
            var r = Jvm.Allocate<PlayerCallbacksRunnable>();
            r.Init(This, Jvm.InternalizeString(PlayerListener.STARTED), l.This, targets);
            var t = Jvm.Allocate<Thread>();
            t.InitTargeted(r.This);
            lock (this)
                listenersPending++;
            t.start();
        }
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
        checkNotClosed();
        var volume = AllocVolomeControl();
        return Jvm.WrapReferenceArray(new[] { volume.This }, "[Ljavax/microedition/media/Control;");
    }

    [return: JavaType(typeof(Control))]
    public Reference getControl([String] Reference type)
    {
        checkNotClosed();
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
        var ctrl = Jvm.Allocate<VolumeControl>();
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
            Toolkit.Logger?.LogRuntime(MTLogLevel.Info, $"Closing player {Handle.Id} during object delete");
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