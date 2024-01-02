// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace java.io;

public class ByteArrayOutputStream : OutputStream
{
    [JavaIgnore] public List<sbyte> buf = new();

    [InitMethod]
    public new void Init()
    {
    }

    [InitMethod]
    public void Init(int size)
    {
    }

    public new void close()
    {
    }

    public void reset() => buf.Clear();

    [JavaDescriptor("()[B")]
    public Reference toByteArray()
    {
        return Jvm.AllocateArray(buf.ToArray(), "[B");
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Encoding.UTF8.GetString(buf.ToArray().ToUnsigned()));
    }

    public void write([JavaType("[B")] Reference b, int off, int len)
    {
        var arr = Jvm.ResolveArray<sbyte>(b);
        buf.AddRange(arr.Skip(off).Take(len));
    }

    public new void write(int b)
    {
        buf.Add((sbyte)(byte)(uint)b);
    }

    public int size() => buf.Count;

    public new void flush()
    {
        // do nothing
    }
}