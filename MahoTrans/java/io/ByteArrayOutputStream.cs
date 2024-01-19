// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace java.io;

public class ByteArrayOutputStream : OutputStream
{
    [JavaIgnore] public sbyte[] _buf = null!;
    public int count;

    [InitMethod]
    public new void Init()
    {
        _buf = new sbyte[32];
    }

    [InitMethod]
    public void Init(int size)
    {
        _buf = new sbyte[size];
    }

    public new void close()
    {
    }

    public void reset() => count = 0;

    [JavaDescriptor("()[B")]
    public Reference toByteArray()
    {
        sbyte[] temp = new sbyte[count];
        System.Array.Copy(_buf, temp, count);
        return Jvm.AllocateArray(temp, "[B");
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Encoding.UTF8.GetString(_buf.ToUnsigned(), 0, count));
    }

    public void write([JavaType("[B")] Reference buf, int off, int len)
    {
        sbyte[] b = Jvm.ResolveArray<sbyte>(buf);
        if(off >= 0 && off <= b.Length && len >= 0 && len <= b.Length - off)
        {
            if (count + len > _buf.Length)
                Expand(len);
            System.Array.Copy(b, off, _buf, count, len);
            count += len;
        }
        else
        {
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        }
    }

    public new void write(int b)
    {
        if (count >= _buf.Length)
            Expand(1);
        _buf[count++] = ((sbyte)(byte)(uint)b);
    }

    public int size() => count;

    public new void flush()
    {
        // do nothing
    }

    private void Expand(int i)
    {
        sbyte[] temp = new sbyte[(count + i) * 2];
        System.Array.Copy(_buf, temp, count);
        _buf = temp;
    }
}