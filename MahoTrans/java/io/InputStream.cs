// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace java.io;

public class InputStream : Object
{
    // methods below are stubs per CLDC docs: https://nikita36078.github.io/J2ME_Docs/docs/midp-2.0/java/io/InputStream.html#close() and so on
    public int available() => 0;

    public void close()
    {
    }

    public bool markSupported() => false;

    public int read()
    {
        throw new AbstractCall();
    }

    [JavaDescriptor("([B)I")]
    public JavaMethodBody read(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.arraylength);
        b.AppendVirtcall("read", "([BII)I");
        b.AppendReturnInt();
        return b.Build(4, 2);
    }

    [JavaDescriptor("([BII)I")]
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

        // let's read first byte:
        b.AppendThis();
        b.AppendVirtcall(nameof(read), typeof(int));
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.istore, 5);

        using (b.AppendGoto(JavaOpcode.ifge)) // if(value<0)
        {
            b.Append(JavaOpcode.iconst_m1);
            b.AppendReturnInt();
        }

        // read ok? Writing to buffer.

        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.iload, 5);
        b.Append(JavaOpcode.bastore);

        // i = off+1
        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.iconst_1);
        b.Append(JavaOpcode.iadd);
        b.Append(JavaOpcode.istore, 4);

        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            using (var tr = b.BeginTry<IOException>())
            {
                b.AppendThis();
                b.AppendVirtcall(nameof(read), typeof(int));
                b.Append(JavaOpcode.dup);
                b.Append(JavaOpcode.istore, 5);

                using (b.AppendGoto(JavaOpcode.ifge)) // if(value<0)
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
                b.Append(JavaOpcode.bastore);

                tr.CatchSection();

                b.Append(JavaOpcode.pop);

                b.Append(JavaOpcode.iload, 4);
                b.Append(JavaOpcode.iload_2);
                b.Append(JavaOpcode.isub);
                b.AppendReturnInt(); // return i-off;
            }

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

    public void mark(int readlimit)
    {
        // does nothing
    }

    [JavaDescriptor("(J)J")]
    public JavaMethodBody skip(JavaClass cls)
    {
        // locals: this, count, i, read
        var b = new JavaMethodBuilder(cls);

        // i=0; read=0;
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lstore_2);
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lstore_3);

        using (var loop = b.BeginLoop(JavaOpcode.iflt))
        {
            b.AppendThis();
            b.AppendVirtcall(nameof(read), typeof(int));
            using (b.AppendGoto(JavaOpcode.ifge))
            {
                // read = i; i = count;
                b.Append(JavaOpcode.lload_2);
                b.Append(JavaOpcode.lstore_3);
                b.Append(JavaOpcode.lload_1);
                b.Append(JavaOpcode.lstore_2);
            }

            // i = i + 1L;
            b.Append(JavaOpcode.lload_2);
            b.Append(JavaOpcode.lconst_1);
            b.Append(JavaOpcode.ladd);
            b.Append(JavaOpcode.lstore_2);

            loop.ConditionSection();

            b.Append(JavaOpcode.lload_2);
            b.Append(JavaOpcode.lload_1);
            b.Append(JavaOpcode.lcmp);
        }

        b.Append(JavaOpcode.lload_3);
        b.Append(JavaOpcode.lconst_0);
        b.Append(JavaOpcode.lcmp);
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.Append(JavaOpcode.lload_3);
            b.AppendReturnLong();
        }

        b.Append(JavaOpcode.lload_1);
        b.AppendReturnLong();

        return b.Build(2, 4);
    }

    public void reset()
    {
        Jvm.Throw<IOException>();
    }
}