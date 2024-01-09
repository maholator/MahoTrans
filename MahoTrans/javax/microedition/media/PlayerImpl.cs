// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Handles;
using MahoTrans.Native;
using Object = java.lang.Object;

namespace javax.microedition.media;

public class Player : Object
{
    [JavaIgnore] public MediaHandle Handle;

    public int State = UNREALIZED;
    private bool inited;

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
        if (State == UNREALIZED || State == REALIZED)
            return;
        if (State == STARTED)
            stop();
        State = REALIZED;
    }

    public void close()
    {
        if (State == CLOSED)
            return;
        if (State == STARTED)
            stop();
        State = CLOSED;
        Toolkit.Media.Dispose(Handle);
    }

    public void setLoopCount(int count)
    {
        if (State == CLOSED)
            Jvm.Throw<IllegalStateException>();
        Toolkit.Media.SetLoopCount(Handle, count);
    }

    public override bool OnObjectDelete()
    {
        if (State == STARTED)
        {
            // we are still playing
            return true;
        }

        if (State != CLOSED)
            Toolkit.Media.Dispose(Handle);

        return false;
    }


    public const int CLOSED = 0;
    public const int PREFETCHED = 300;
    public const int REALIZED = 200;
    public const int STARTED = 400;
    public const long TIME_UNKNOWN = -1l;
    public const int UNREALIZED = 100;
}