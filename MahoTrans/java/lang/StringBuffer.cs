using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class StringBuffer : Object
{
    [JavaIgnore] private List<char> _buffer = null!;

    [InitMethod]
    public void Init() => _buffer = new List<char>();

    [InitMethod]
    public void Init(int cap) => _buffer = new List<char>(cap);

    [InitMethod]
    public void InitFromString([String] Reference str)
    {
        _buffer = new List<char>(Heap.ResolveString(str));
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(bool z)
    {
        _buffer.AddRange(z ? "true" : "false");
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(char c)
    {
        _buffer.Add(c);
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append([String] Reference s)
    {
        _buffer.AddRange(Heap.ResolveString(s));
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(double d)
    {
        _buffer.AddRange(d.ToString());
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(float f)
    {
        _buffer.AddRange(f.ToString());
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(int i)
    {
        _buffer.AddRange(i.ToString());
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(long l)
    {
        _buffer.AddRange(l.ToString());
        return This;
    }

    public int length() => _buffer.Count;

    [return: String]
    public Reference toString()
    {
        return Heap.AllocateString(new string(_buffer.ToArray()));
    }
}