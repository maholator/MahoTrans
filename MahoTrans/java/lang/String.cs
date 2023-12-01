using System.Globalization;
using System.Text;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.lang;

public sealed class String : Object
{
    [JavaIgnore] public string Value = null!;

    #region Constructors

    [InitMethod]
    [JavaDescriptor("([B)V")]
    public void InitBytes(Reference arr)
    {
        var buf = Jvm.ResolveArray<sbyte>(arr).ToUnsigned();
        Value = Encoding.UTF8.GetString(buf);
    }

    [InitMethod]
    [JavaDescriptor("([BII)V")]
    public void InitBytes(Reference arr, int from, int len)
    {
        var span = new ReadOnlySpan<byte>(Jvm.ResolveArray<sbyte>(arr).ToUnsigned(), from, len);
        Value = Encoding.UTF8.GetString(span);
    }

    [InitMethod]
    public void InitBytes([JavaType("[B")] Reference arr, int from, int len, [String] Reference enc)
    {
        var span = new ReadOnlySpan<byte>(Jvm.ResolveArray<sbyte>(arr).ToUnsigned(), from, len);
        Value = Jvm.ResolveString(enc).GetEncodingByName().GetString(span);
    }

    [InitMethod]
    public void InitBytes([JavaType("[B")] Reference arr, [String] Reference enc)
    {
        var buf = Jvm.ResolveArray<sbyte>(arr).ToUnsigned();
        Value = Jvm.ResolveString(enc).GetEncodingByName().GetString(buf);
    }

    [InitMethod]
    public new void Init()
    {
        Value = string.Empty;
    }

    [InitMethod]
    [JavaDescriptor("([C)V")]
    public void Init(Reference charArr)
    {
        Value = new string(Jvm.ResolveArray<char>(charArr));
    }

    [InitMethod]
    [JavaDescriptor("([CII)V")]
    public void Init(Reference charArr, int from, int len)
    {
        Value = new string(Jvm.ResolveArray<char>(charArr), from, len);
    }

    [InitMethod]
    public void InitCopy([String] Reference value)
    {
        Value = new string(Jvm.ResolveString(value));
    }

    #endregion

    public int length()
    {
        return Value.Length;
    }

    public bool startsWith([JavaType(typeof(String))] Reference s)
    {
        var other = Jvm.ResolveString(s);
        return Value.StartsWith(other);
    }

    public bool startsWith([String] Reference prefix, int from)
    {
        var other = Jvm.ResolveString(prefix);
        return Value.IndexOf(other, from, StringComparison.Ordinal) == from;
    }

    public bool endsWith([JavaType(typeof(String))] Reference s)
    {
        var other = Jvm.ResolveString(s);
        return Value.EndsWith(other);
    }

    public char charAt(int i)
    {
        return Value[i];
    }

    [JavaDescriptor("()[B")]
    public Reference getBytes()
    {
        var data = Encoding.UTF8.GetBytes(Value).ConvertToSigned();
        return Jvm.AllocateArray(data, "[B");
    }

    [JavaDescriptor("(Ljava/lang/String;)[B")]
    public Reference getBytes(Reference enc)
    {
        byte[] data = Jvm.ResolveString(enc).GetEncodingByName().GetBytes(Value);
        return Jvm.AllocateArray(data.ConvertToSigned(), "[B");
    }

    [return: JavaType("[C")]
    public Reference toCharArray()
    {
        return Jvm.AllocateArray(Value.ToCharArray(), "[C");
    }

    [return: JavaType(typeof(String))]
    public Reference toString() => This;

    public new bool equals(Reference r)
    {
        if (r.IsNull)
            return false;

        var obj = Jvm.ResolveObject(r);

        if (obj is String s)
        {
            return s.Value == Value;
        }

        return false;
    }

    //TODO add unit tests on this
    public new int hashCode()
    {
        // s[0]*31^(n-1) + s[1]*31^(n-2) + ... + s[n-1]
        int hash = 0;
        int n = Value.Length;
        int pow = 0;

        if (n > 0)
        {
            hash = Value[n - 1];
            pow = 1;
        }

        for (int i = n - 2; i >= 0; i--)
        {
            pow *= 31;
            hash += pow * Value[i];
        }

        return hash;
    }

    public bool equalsIgnoreCase([String] Reference anotherString)
    {
        string s2 = Jvm.ResolveString(anotherString);
        return Value.ToLower(CultureInfo.InvariantCulture) == s2.ToLower(CultureInfo.InvariantCulture);
    }


    public int compareTo([String] Reference anotherString)
    {
        string s2 = Jvm.ResolveString(anotherString);

        var len = Math.min(s2.Length, Value.Length);

        for (int k = 0; k < len; k++)
        {
            int diff = Value[k] - s2[k];
            if (diff != 0)
                return diff;
        }

        return Value.Length - s2.Length;
    }

    public int indexOf(int c) => Value.IndexOf((char)c);

    public int indexOf(int c, int from) => Value.IndexOf((char)c, from);

    public int indexOf([String] Reference strr)
    {
        var str = Jvm.ResolveString(strr);
        return Value.IndexOf(str, StringComparison.Ordinal);
    }

    public int indexOf([String] Reference strr, int from)
    {
        var str = Jvm.ResolveString(strr);
        return Value.IndexOf(str, from, StringComparison.Ordinal);
    }

    [return: String]
    public Reference substring(int from)
    {
        return Jvm.AllocateString(Value.Substring(from));
    }

    [return: String]
    public Reference substring(int from, int to)
    {
        return Jvm.AllocateString(Value.Substring(from, to - from));
    }

    [return: String]
    public Reference toUpperCase() => Jvm.AllocateString(Value.ToUpper());

    [return: String]
    public Reference toLowerCase() => Jvm.AllocateString(Value.ToLower());

    [return: String]
    public Reference replace(char from, char to) => Jvm.AllocateString(Value.Replace(from, to));

    [return: String]
    public Reference trim() => Jvm.AllocateString(Value.Trim());

    #region valueOf

    [return: String]
    public static Reference valueOf(bool v) => Jvm.AllocateString(v ? "true" : "false");

    [return: String]
    public static Reference valueOf(char v) => Jvm.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf([JavaType("[C")] Reference charArr) =>
        Jvm.AllocateString(new string(Jvm.ResolveArray<char>(charArr)));

    [return: String]
    public static Reference valueOf([JavaType("[C")] Reference charArr, int from, int count) =>
        Jvm.AllocateString(new string(Jvm.ResolveArray<char>(charArr), from, count));

    [return: String]
    public static Reference valueOf(double v) => Jvm.AllocateString(v.ToString(CultureInfo.InvariantCulture));

    [return: String]
    public static Reference valueOf(float v) => Jvm.AllocateString(v.ToString(CultureInfo.InvariantCulture));

    [return: String]
    public static Reference valueOf(int v) => Jvm.AllocateString(v.ToString());

    [return: String]
    public static Reference valueOf(long v) => Jvm.AllocateString(v.ToString());

    [JavaDescriptor("(Ljava/lang/Object;)Ljava/lang/String;")]
    public static JavaMethodBody valueOf(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 1,
            Code = new Instruction[]
            {
                new(0, JavaOpcode.aload_0),
                new(1, JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("toString", "()Ljava/lang/String;", "java/lang/Object"))
                        .Split()),
                new(4, JavaOpcode.areturn)
            }
        };
    }

    #endregion
}