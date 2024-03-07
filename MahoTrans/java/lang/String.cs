// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Text;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.lang;

public sealed class String : Object
{
    [JavaIgnore]
    public string Value = null!;

    #region Constructors

    [InitMethod]
    public void InitBytes(sbyte[] arr)
    {
        var buf = arr.ToUnsigned();
        Value = Encoding.UTF8.GetString(buf);
    }

    [InitMethod]
    public void InitBytes(sbyte[] arr, int from, int len)
    {
        var span = new ReadOnlySpan<byte>(arr.ToUnsigned(), from, len);
        Value = Encoding.UTF8.GetString(span);
    }

    [InitMethod]
    public void InitBytes(sbyte[] arr, int from, int len, string enc)
    {
        var span = new ReadOnlySpan<byte>(arr.ToUnsigned(), from, len);
        Value = enc.GetEncodingByName().GetString(span);
    }

    [InitMethod]
    public void InitBytes(sbyte[] arr, [String] Reference enc)
    {
        var buf = arr.ToUnsigned();
        Value = Jvm.ResolveString(enc).GetEncodingByName().GetString(buf);
    }

    [InitMethod]
    public new void Init()
    {
        Value = string.Empty;
    }

    [InitMethod]
    public void Init(char[] charArr)
    {
        Value = new string(charArr);
    }

    [InitMethod]
    public void Init(char[] charArr, int from, int len)
    {
        Value = new string(charArr, from, len);
    }

    [InitMethod]
    public void InitCopy(string value)
    {
        Value = value;
    }

    [InitMethod]
    public void InitFromBuffer([JavaType(typeof(StringBuffer))] Reference buf)
    {
        Value = Jvm.Resolve<StringBuffer>(buf).ToString();
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

    public bool startsWith(string prefix, int from)
    {
        return Value.IndexOf(prefix, from, StringComparison.Ordinal) == from;
    }

    public bool endsWith(string s)
    {
        return Value.EndsWith(s);
    }

    public char charAt(int i)
    {
        return Value[i];
    }

    [JavaDescriptor("()[B")]
    public Reference getBytes()
    {
        var data = Encoding.UTF8.GetBytes(Value).ConvertToSigned();
        return Jvm.WrapPrimitiveArray(data);
    }

    [JavaDescriptor("(Ljava/lang/String;)[B")]
    public Reference getBytes(string enc)
    {
        byte[] data = enc.GetEncodingByName().GetBytes(Value);
        return Jvm.WrapPrimitiveArray(data.ConvertToSigned());
    }

    [return: JavaType("[C")]
    public Reference toCharArray()
    {
        return Jvm.WrapPrimitiveArray(Value.ToCharArray());
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

    public int lastIndexOf(int c) => Value.LastIndexOf((char)c);

    public int lastIndexOf(int c, int from) => Value.LastIndexOf((char)c, from);

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

    [return: String]
    public Reference intern() => Jvm.InternalizeString(Value);

    [return: String]
    public Reference concat([String] Reference str)
    {
        var s = Jvm.ResolveString(str);
        return Jvm.AllocateString(Value + s);
    }

    public void getChars(int srcBegin, int srcEnd, [JavaType("[C")] Reference dest, int destBegin)
    {
        Value.CopyTo(srcBegin, Jvm.ResolveArray<char>(dest), destBegin, srcEnd - srcBegin);
    }

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
    public static JavaMethodBody valueOf(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendConstant("null");
            b.AppendReturnReference();
        }

        b.AppendThis();
        b.AppendVirtcall("toString", typeof(String));
        b.AppendReturnReference();

        return b.Build(1, 1);
    }

    #endregion
}
