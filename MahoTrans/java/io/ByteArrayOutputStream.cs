using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace java.io;

public class ByteArrayOutputStream : OutputStream
{
    [JavaIgnore] public List<sbyte> buf = new();

    [InitMethod]
    public void Init()
    {
    }

    [InitMethod]
    public void Init(int size)
    {
    }

    public void close()
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
        return Jvm.AllocateString(buf.ToArray().ToUnsigned().DecodeDefault());
    }

    public void write([JavaType("[B")] Reference b, int off, int len)
    {
        var arr = Jvm.ResolveArray<sbyte>(b);
        buf.AddRange(arr.Skip(off).Take(len));
    }

    public void write(int b)
    {
        buf.Add((sbyte)(byte)(uint)b);
    }

    public int size() => buf.Count;

    public void flush()
    {
        // do nothing
    }
}