// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using MahoTrans;
using MahoTrans.Builder;
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
    public new void Init() => _buffer = new List<char>();

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
    public Reference append___chars([JavaType("[C")] Reference arr)
    {
        var s = Jvm.ResolveArray<char>(arr);
        _buffer.AddRange(s);
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append___chars([JavaType("[C")] Reference arr, int offset, int len)
    {
        var s = Jvm.ResolveArray<char>(arr);
        _buffer.AddRange(s.Skip(offset).Take(len));
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(double d)
    {
        _buffer.AddRange(d.ToString(CultureInfo.InvariantCulture));
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference append(float f)
    {
        _buffer.AddRange(f.ToString(CultureInfo.InvariantCulture));
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
    public Reference append([String] Reference s)
    {
        _buffer.AddRange(Jvm.ResolveStringOrNull(s) ?? "null");
        return This;
    }

    public int capacity() => _buffer.Capacity;

    public char charAt(int index) => _buffer[index];

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

    [return: JavaType(typeof(StringBuffer))]
    public Reference deleteCharAt(int index)
    {
        if (index < 0 || index >= _buffer.Count)
            Jvm.Throw<StringIndexOutOfBoundsException>();
        _buffer.RemoveAt(index);
        return This;
    }

    public void ensureCapacity(int minCapacity)
    {
        if (_buffer.Capacity < minCapacity)
            _buffer.Capacity = minCapacity;
    }

    public void getChars(int srcBegin, int srcEnd, [JavaType("[C")] Reference dstRef, int dstBegin)
    {
        var dst = Jvm.ResolveArray<char>(dstRef);

        if (srcBegin < 0 || srcBegin > _buffer.Count || srcEnd < 0 || srcEnd > _buffer.Count)
            Jvm.Throw<IndexOutOfBoundsException>();

        _buffer.CopyTo(srcBegin, dst, dstBegin, srcEnd - srcBegin);
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, bool z)
    {
        _buffer.InsertRange(offset, z ? "true" : "false");
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, char c)
    {
        _buffer.Insert(offset, c);
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert___chars(int offset, [JavaType("[C")] Reference arr)
    {
        var s = Jvm.ResolveArray<char>(arr);
        _buffer.InsertRange(offset, s);
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, double d)
    {
        _buffer.InsertRange(offset, d.ToString(CultureInfo.InvariantCulture));
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, float f)
    {
        _buffer.InsertRange(offset, f.ToString(CultureInfo.InvariantCulture));
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, int i)
    {
        _buffer.InsertRange(offset, i.ToString());
        return This;
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, long l)
    {
        _buffer.InsertRange(offset, l.ToString());
        return This;
    }

    [JavaDescriptor("(ILjava/lang/Object;)Ljava/lang/StringBuffer;")]
    public JavaMethodBody insert(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);

        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.Append(JavaOpcode.aload_2);
        b.AppendVirtcall("toString", typeof(String));
        b.AppendVirtcall(nameof(insert), "(ILjava/lang/String;)Ljava/lang/StringBuffer;");
        b.AppendThis();
        b.AppendReturnReference();

        return b.Build(3, 3);
    }

    [return: JavaType(typeof(StringBuffer))]
    public Reference insert(int offset, [String] Reference s)
    {
        _buffer.InsertRange(offset, Jvm.ResolveString(s));
        return This;
    }

    public int length() => _buffer.Count;

    [return: JavaType(typeof(StringBuffer))]
    public Reference reverse()
    {
        _buffer.Reverse();
        return This;
    }

    public void setCharAt(int index, char c)
    {
        _buffer[index] = c;
    }

    public void setLength(int newLength)
    {
        if (_buffer.Count == newLength)
            return;
        if (_buffer.Count < newLength)
        {
            _buffer.AddRange(Enumerable.Repeat('\0', newLength - _buffer.Count));
            return;
        }

        _buffer.RemoveRange(newLength, _buffer.Count - newLength);
    }

    [JavaIgnore]
    public override string ToString()
    {
        return new string(_buffer.ToArray());
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(new string(_buffer.ToArray()));
    }
}