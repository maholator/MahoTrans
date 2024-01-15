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

public class OutputStream : Object
{
    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    public void close()
    {
    }

    public void flush()
    {
    }

    public void write(int b)
    {
        throw new AbstractJavaMethodCallError();
    }

    [JavaDescriptor("([B)V")]
    public JavaMethodBody write(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.arraylength);
        b.AppendVirtcall("write", "([BII)V");
        b.AppendReturn();
        return b.Build(4, 2);
    }

    //TODO verify this impl
    [JavaDescriptor("([BII)V")]
    public JavaMethodBody write___bounds(JavaClass cls)
    {
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

        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.istore, 4);

        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.AppendThis();
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.baload);
            b.AppendVirtcall(nameof(write), typeof(void), typeof(int));

            b.AppendInc(4, 1);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload_3);
            b.Append(JavaOpcode.iadd);
        }

        b.AppendReturn();

        return b.Build(3, 5);
    }
}