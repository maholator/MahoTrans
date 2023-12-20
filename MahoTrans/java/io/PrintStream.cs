using System.Text;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using String = java.lang.String;

namespace java.io;

public class PrintStream : OutputStream
{
    [JavaType(typeof(OutputStream))] public Reference Output;

    [InitMethod]
    public void Init([JavaType(typeof(OutputStream))] Reference r) => Output = r;

    [return: JavaType("[B")]
    public static Reference ToBytes(char c)
    {
        char[] chars = new char[] { c };
        var bytes = Encoding.UTF8.GetBytes(chars).ConvertToSigned();
        return Jvm.AllocateArray(bytes, "[B");
    }

    public bool checkError() => false;

    [JavaDescriptor("()V")]
    public JavaMethodBody close(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Output), typeof(OutputStream));
        b.AppendVirtcall("close", "()V");
        b.AppendReturn();
        return b.Build(1, 1);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody flush(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Output), typeof(OutputStream));
        b.AppendVirtcall("flush", "()V");
        b.AppendReturn();
        return b.Build(1, 1);
    }

    [JavaDescriptor("(Z)V")]
    public JavaMethodBody print___bool(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendStaticCall<String>(nameof(String.valueOf), typeof(String), typeof(bool));
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(C)V")]
    public JavaMethodBody print___char(JavaClass cls)
    {
        // locals: this > char > i > []buf
        var b = new JavaMethodBuilder(cls);

        b.Append(JavaOpcode.iload_1);
        b.AppendStaticCall(new NameDescriptorClass(nameof(ToBytes), "(C)[B", typeof(PrintStream)));
        b.Append(JavaOpcode.astore_3);

        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_2);

        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.AppendThis();
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.baload);
            b.AppendVirtcall("write", "(I)V");
            b.AppendInc(2, 1);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.arraylength);
        }

        b.AppendReturn();
        return b.Build(3, 4);
    }

    [JavaDescriptor("([C)V")]
    public JavaMethodBody print___chars(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_2);
        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.AppendThis();
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.caload);
            b.AppendVirtcall("print", "(C)V");
            b.AppendInc(2, 1);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.arraylength);
        }

        b.AppendReturn();

        return b.Build(3, 3);
    }

    [JavaDescriptor("(D)V")]
    public JavaMethodBody print___double(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.dload_1);
        b.AppendStaticCall<String>(nameof(String.valueOf), typeof(String), typeof(double));
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(F)V")]
    public JavaMethodBody print___float(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.fload_1);
        b.AppendStaticCall<String>(nameof(String.valueOf), typeof(String), typeof(float));
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody print___int(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendStaticCall<String>(nameof(String.valueOf), typeof(String), typeof(int));
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(J)V")]
    public JavaMethodBody print___long(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.lload_1);
        b.AppendStaticCall<String>(nameof(String.valueOf), typeof(String), typeof(long));
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/Object;)V")]
    public JavaMethodBody print___object(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("toString", "()Ljava/lang/String;");
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;)V")]
    public JavaMethodBody print___string(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendConstant("null");
            b.AppendVirtcall("print", "(Ljava/lang/String;)V");
            b.AppendReturn();
        }

        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(String.toCharArray), "()[C");
        b.AppendVirtcall("print", "([C)V");
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

    [JavaDescriptor("(Z)V")]
    public JavaMethodBody println___bool(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("print", "(Z)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(C)V")]
    public JavaMethodBody println___char(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("print", "(C)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("([C)V")]
    public JavaMethodBody println___chars(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("print", "([C)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(D)V")]
    public JavaMethodBody println___double(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.dload_1);
        b.AppendVirtcall("print", "(D)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(F)V")]
    public JavaMethodBody println___float(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.fload_1);
        b.AppendVirtcall("print", "(F)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody println___int(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("print", "(I)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(J)V")]
    public JavaMethodBody println___long(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.lload_1);
        b.AppendVirtcall("print", "(J)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/Object;)V")]
    public JavaMethodBody println___object(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("toString", "()Ljava/lang/String;");
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;)V")]
    public JavaMethodBody println___string(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("print", "(Ljava/lang/String;)V");
        b.AppendThis();
        b.AppendVirtcall("println", typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    public void setError()
    {
    }

    //TODO write([BII)V

    [JavaDescriptor("(I)V")]
    public new JavaMethodBody write(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(Output), typeof(OutputStream));
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("write", "(I)V");
        b.AppendReturn();
        return b.Build(2, 2);
    }
}