using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.lang;

public sealed class String : Object
{
    [JavaIgnore] public string Value = null!;

    [InitMethod]
    [JavaDescriptor("([B)V")]
    public void InitBytes(Reference arr)
    {
        var buf = Heap.ResolveArray<sbyte>(arr).ToUnsigned();
        Value = buf.DecodeDefault();
    }

    [InitMethod]
    public void Init()
    {
        Value = string.Empty;
    }

    public int length()
    {
        return Value.Length;
    }

    public bool startsWith([JavaType(typeof(String))] Reference s)
    {
        var other = Heap.ResolveString(s);
        return Value.StartsWith(other);
    }

    public bool endsWith([JavaType(typeof(String))] Reference s)
    {
        var other = Heap.ResolveString(s);
        return Value.EndsWith(other);
    }

    public char charAt(int i)
    {
        return Value[i];
    }

    [JavaDescriptor("()[B")]
    public Reference getBytes()
    {
        var data = Value.EncodeDefault().ToSigned();
        return Heap.AllocateArray(data);
    }

    [JavaDescriptor("(Ljava/lang/String;)[B")]
    public Reference getBytes(Reference enc)
    {
        //TODO
        var data = Value.EncodeUTF8().ToSigned();
        return Heap.AllocateArray(data);
    }

    [return: JavaType(typeof(String))]
    public Reference toString() => This;

    public bool equals(Reference r)
    {
        if (r.IsNull)
            return false;

        var obj = Heap.ResolveObject(r);

        if (obj is String s)
        {
            return s.Value == Value;
        }

        return false;
    }

    public int indexOf(int c) => Value.IndexOf((char)c);

    public int indexOf(int c, int from) => Value.IndexOf((char)c, from);

    public int indexOf([String] Reference strr, int from)
    {
        var str = Heap.ResolveString(strr);
        return Value.IndexOf(str, from, StringComparison.Ordinal);
    }

    [return: String]
    public Reference substring(int from)
    {
        return Heap.AllocateString(Value.Substring(from));
    }

    [return: String]
    public Reference substring(int from, int to)
    {
        return Heap.AllocateString(Value.Substring(from, to - from));
    }

    #region valueOf

    [return: String]
    public static Reference valueOf(bool v) => Heap.AllocateString(v ? "true" : "false");

    [return: String]
    public static Reference valueOf(char v) => Heap.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf([JavaType("[C")] Reference charArr) =>
        Heap.AllocateString(new string(Heap.ResolveArray<char>(charArr)));

    [return: String]
    public static Reference valueOf([JavaType("[C")] Reference charArr, int from, int count) =>
        Heap.AllocateString(new string(Heap.ResolveArray<char>(charArr), from, count));

    [return: String]
    public static Reference valueOf(double v) => Heap.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf(float v) => Heap.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf(int v) => Heap.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf(long v) => Heap.AllocateString(v.ToString());

    [JavaDescriptor("(Ljava/lang/Object;)Ljava/lang/String;")]
    public static JavaMethodBody valueOf(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 1,
            Code = new Instruction[]
            {
                new Instruction(0, JavaOpcode.aload_0),
                new Instruction(1, JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("toString", "()Ljava/lang/String;", "java/lang/Object"))
                        .Split()),
                new Instruction(4, JavaOpcode.areturn)
            }
        };
    }

    #endregion
}