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

    public void reset()
    {
        Jvm.Throw<IOException>();
    }
}