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
        throw new AbstractJavaMethodCallError();
    }

    [JavaDescriptor("([B)I")]
    public JavaMethodBody read(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("read", "([BII)I");
        b.AppendReturnInt();
        return b.Build(4, 2);
    }

    public void mark(int readlimit)
    {
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