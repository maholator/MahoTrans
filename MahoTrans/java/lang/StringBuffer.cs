using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.lang;

public class StringBuffer : Object
{
    [JavaIgnore] [JsonProperty] private List<char> _buffer = null!;

    [InitMethod]
    public void Init() => _buffer = new List<char>();

    [InitMethod]
    public void Init(int cap) => _buffer = new List<char>(cap);

    [InitMethod]
    public void InitFromString([String] Reference str)
    {
        _buffer = new List<char>(Jvm.ResolveString(str));
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
        _buffer.AddRange(Jvm.ResolveString(s));
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

    [JavaDescriptor("(Ljava/lang/Object;)Ljava/lang/StringBuffer;")]
    public JavaMethodBody append(JavaClass cls)
    {
        // this, arg
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("toString", "()Ljava/lang/String;")).Split()),
                new(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("append", "(Ljava/lang/String;)Ljava/lang/StringBuffer;"))
                        .Split()),
                new(JavaOpcode.areturn),
            }
        };
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference delete(int start, int end)
    {
        if (start == end)
        {
            // no changes
            return This;
        }

        if (start < 0 || start > _buffer.Count || start > end)
            Jvm.Throw<StringIndexOutOfBoundsException>();

        _buffer = _buffer.Take(start).Concat(_buffer.Skip(end)).ToList();
        return This;
    }

    public char charAt(int index) => _buffer[index];

    [return: JavaType(typeof(StringBuffer))]
    public Reference deleteCharAt(int index)
    {
        if (index < 0 || index >= _buffer.Count)
            Jvm.Throw<StringIndexOutOfBoundsException>();
        _buffer.RemoveAt(index);
        return This;
    }


    public int length() => _buffer.Count;

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(new string(_buffer.ToArray()));
    }
}