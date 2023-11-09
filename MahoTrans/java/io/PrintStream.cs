using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.io;

public class PrintStream : OutputStream
{
    [JavaType(typeof(OutputStream))] public Reference Output;

    [InitMethod]
    public void Init([JavaType(typeof(OutputStream))] Reference r) => Output = r;

    [return: JavaType("[B")]
    public Reference ToBytes([String] Reference r)
    {
        var buf = Jvm.ResolveString(r).EncodeUTF8();
        return Jvm.AllocateArray(buf, "[B");
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody write(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Output), typeof(OutputStream));
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("write", "(I)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(C)V")]
    public JavaMethodBody print(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendConstant(8);
        b.Append(JavaOpcode.ishr);
        b.AppendVirtcall("write", "(I)V");
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("write", "(I)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody println(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendConstant('\n');
        b.AppendVirtcall("print", "(C)V");
        b.AppendReturn();
        return b.Build(2, 1);
    }
}