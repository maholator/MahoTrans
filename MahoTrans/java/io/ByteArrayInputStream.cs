// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Array = java.lang.Array;

namespace java.io;

public class ByteArrayInputStream : InputStream
{
    // as per MIDP docs
    [JavaType("[B")]
    public Reference buf;

    public int count;
    public int markPos;
    public int pos;

    [InitMethod]
    [JavaDescriptor("([B)V")]
    public void Init(Reference r)
    {
        buf = r;
        pos = 0;
        count = Jvm.Resolve<Array>(r).BaseArray.Length;
    }

    [InitMethod]
    [JavaDescriptor("([BII)V")]
    public void Init(Reference r, int offset, int len)
    {
        buf = r;
        pos = offset;
        count = len + offset;
    }

    public new int read()
    {
        if (pos == count)
            return -1;
        var b = Jvm.ResolveArray<sbyte>(buf)[pos];
        pos++;
        return (byte)b;
    }

    [JavaDescriptor("([B)I")]
    public int read(Reference arr)
    {
        if (pos == count)
            return -1;
        var b = Jvm.ResolveArray<sbyte>(buf);
        var target = Jvm.ResolveArray<sbyte>(arr);
        int read = Math.Min(target.Length, count - pos);

        for (int i = 0; i < read; i++)
            target[i] = b[pos + i];

        pos += read;
        return read;
    }

    [JavaDescriptor("([BII)I")]
    public int read(Reference arr, int from, int len)
    {
        if (pos == count)
            return -1;
        int read = Math.Min(len, count - pos);
        var b = Jvm.ResolveArray<sbyte>(buf);
        var target = Jvm.ResolveArray<sbyte>(arr);

        for (int i = 0; i < read; i++)
            target[from + i] = b[pos + i];

        pos += read;
        return read;
    }

    public long skip(long n)
    {
        long read = Math.Min(n, count - pos);
        pos = (int)(pos + read);
        return read;
    }

    public new void mark(int readlimit)
    {
        markPos = pos;
    }

    public new void reset()
    {
        pos = markPos;
    }

    public new bool markSupported() => true;

    public new int available() => count - pos;

    public new void close()
    {
        // do nothing
    }
}
