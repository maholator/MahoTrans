// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.io;

public class InputStreamReader : Reader
{
    [JavaType(typeof(InputStream))]
    public Reference Stream;

    [JavaIgnore]
    [JsonProperty]
    private Decoder _decoder = null!;

    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference stream)
    {
        Stream = stream;
        _decoder = "UTF-8".GetEncodingByName().GetDecoder();
    }

    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference stream, [String] Reference enc)
    {
        Stream = stream;
        _decoder = (Jvm.ResolveStringOrNull(enc) ?? "UTF-8").GetEncodingByName().GetDecoder();
    }

    public int decodeChar(int b)
    {
        // end of stream
        if (b == -1)
            return -1;

        Span<byte> arr = stackalloc byte[1];
        arr[0] = (byte)((uint)b & 0xFF);
        Span<char> result = stackalloc char[1];

        var count = _decoder.GetChars(arr, result, false);
        if (count == 1)
            return result[0];

        return -2;
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody read(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        var begin = b.PlaceLabel();
        b.AppendThis();
        b.AppendThis();
        b.AppendGetLocalField(nameof(Stream), typeof(InputStream));
        b.AppendVirtcall("read", typeof(int));
        b.AppendVirtcall(nameof(decodeChar), typeof(int), typeof(int));
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.istore_1);
        b.Append(JavaOpcode.bipush, 0xFE); // -2
        // -2 is returned by decoder if existing data is not enough to decode a char. Reading one more.
        b.AppendGoto(JavaOpcode.if_icmpeq, begin);

        b.Append(JavaOpcode.iload_1); // here is -1, 0 or valid UTF16 character. All are good.
        b.AppendReturnInt();
        return b.Build(2, 2);
    }

    [JavaDescriptor("([CII)I")]
    public JavaMethodBody read___bounds(JavaClass cls)
    {
        // locals: this, buf, off, len, i, value
        //         0     1    2    3    4  5

        var b = new JavaMethodBuilder(cls);

        // If off is negative...
        b.Append(JavaOpcode.iload_2);
        using (b.AppendGoto(JavaOpcode.ifge))
        {
            b.AppendNewObject<IndexOutOfBoundsException>();
            b.Append(JavaOpcode.athrow);
        }

        // ...or len is negative...
        b.Append(JavaOpcode.iload_3);
        using (b.AppendGoto(JavaOpcode.ifge))
        {
            b.AppendNewObject<IndexOutOfBoundsException>();
            b.Append(JavaOpcode.athrow);
        }

        // ...or off+len is greater than the length of the array b...
        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.iload_3);
        b.Append(JavaOpcode.iadd);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.arraylength);
        using (b.AppendGoto(JavaOpcode.if_icmple))
        {
            b.AppendNewObject<IndexOutOfBoundsException>();
            b.Append(JavaOpcode.athrow);
        }
        // ...then an IndexOutOfBoundsException is thrown.

        // If len is zero, then no bytes are read and 0 is returned.
        b.Append(JavaOpcode.iload_3);
        using (b.AppendGoto(JavaOpcode.ifne))
        {
            b.Append(JavaOpcode.iconst_0);
            b.AppendReturnInt();
        }

        // i = off
        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.istore, 4);

        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.AppendThis();
            b.AppendVirtcall(nameof(read), typeof(int));
            b.Append(JavaOpcode.dup);
            b.Append(JavaOpcode.istore, 5);

            using (b.AppendGoto(JavaOpcode.ifge)) // if(value==-1)
            {
                b.Append(JavaOpcode.iload, 4);
                b.Append(JavaOpcode.iload_2);
                b.Append(JavaOpcode.isub); // readCount = i-off;
                b.Append(JavaOpcode.dup);
                using (b.AppendGoto(JavaOpcode.ifgt))
                {
                    // if read count is zero, return -1 because we read nothing.
                    b.Append(JavaOpcode.pop);
                    b.Append(JavaOpcode.iconst_m1);
                }

                b.AppendReturnInt();
            }

            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iload, 5);
            b.Append(JavaOpcode.i2c);
            b.Append(JavaOpcode.castore);

            b.AppendInc(4, 1);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload_3);
            b.Append(JavaOpcode.iadd);
        }

        b.Append(JavaOpcode.iload_3);
        b.AppendReturnInt();

        return b.Build(3, 6);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody close(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Stream), typeof(InputStream));
        b.AppendVirtcall("close", typeof(void));
        b.AppendReturn();
        return b.Build(1, 1);
    }
}
