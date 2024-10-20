// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.io;

public class DataOutputStream : OutputStream
{
    [JavaType(typeof(OutputStream))]
    public Reference @out;

    [JavaIgnore]
    private readonly NameDescriptorClass _streamDescriptor =
        new("out", typeof(OutputStream), typeof(DataOutputStream));

    [JavaIgnore]
    private readonly NameDescriptorClass _write = new NameDescriptorClass("write", "(I)V", typeof(DataOutputStream));

    [InitMethod]
    public void Init([JavaType(typeof(OutputStream))] Reference r)
    {
        @out = r;
    }

    /// <summary>
    ///     Writes one byte into <see cref="@out" />. Do all operations via this method.
    /// </summary>
    /// <param name="cls"></param>
    /// <returns></returns>
    [JavaDescriptor("(I)V")]
    public JavaMethodBody write___byte(JavaClass cls)
    {
        JavaMethodBuilder j = new JavaMethodBuilder(cls);
        j.AppendThis();
        j.AppendGetLocalField("out", typeof(OutputStream));
        j.Append(JavaOpcode.iload_1);
        j.AppendVirtcall("write", "(I)V");
        j.AppendReturn();
        return j.Build(2, 2);
    }

    [JavaDescriptor("([B)V")]
    public JavaMethodBody write___buf(JavaClass cls)
    {
        byte[] streamWrite = cls.PushConstant(new NameDescriptor("write", "(I)V")).Split();
        return new JavaMethodBody(3, 3)
        {
            // Locals: this > arr > index
            RawCode = new Instruction[]
            {
                // i=0
                new(JavaOpcode.iconst_0),
                new(JavaOpcode.istore_2),

                new(JavaOpcode.@goto, new byte[] { 0, 16 }),

                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(_streamDescriptor).Split()),
                // > stream
                new(JavaOpcode.aload_1),
                // > stream > arr
                new(JavaOpcode.iload_2),
                // > stream > arr > i
                new(JavaOpcode.baload),
                // > stream > value
                new(JavaOpcode.invokevirtual, streamWrite),
                // i++
                new(JavaOpcode.iinc, new byte[] { 2, 1 }),

                // if(i<buf.len) goto
                new(JavaOpcode.iload_2),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.arraylength),
                // > i > len
                new(JavaOpcode.if_icmplt, (-16).Split()),

                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("([BII)V")]
    public JavaMethodBody write___bufExplicit(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var loop = b.BeginLoop(JavaOpcode.if_icmplt))
        {
            b.AppendThis();
            b.AppendGetLocalField(nameof(@out), typeof(OutputStream));
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.baload);
            b.AppendVirtcall("write", "(I)V");
            b.AppendInc(2, 1);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload_3);
        }

        b.AppendReturn();
        return b.Build(3, 4);
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody writeByte(JavaClass cls) => write___byte(cls);

    [JavaDescriptor("(Z)V")]
    public JavaMethodBody writeBoolean(JavaClass cls) => write___byte(cls);

    [JavaDescriptor("(I)V")]
    public JavaMethodBody writeInt(JavaClass cls)
    {
        byte[] streamWrite = cls.PushConstant(_write).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.bipush, new byte[] { 24 }),
                new(JavaOpcode.ishr),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.bipush, new byte[] { 16 }),
                new(JavaOpcode.ishr),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.ishr),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("(J)V")]
    public JavaMethodBody writeLong(JavaClass cls)
    {
        byte[] streamWrite = cls.PushConstant(_write).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 56 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 48 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 40 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 32 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 24 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 16 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.lshr),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.l2i),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody writeShort(JavaClass cls)
    {
        byte[] streamWrite = cls.PushConstant(_write).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.ishr),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("(F)V")]
    public JavaMethodBody writeFloat(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.fload_1);
        b.AppendStaticCall<Float>(nameof(Float.floatToIntBits), typeof(int), typeof(float));
        b.AppendVirtcall(nameof(writeInt), typeof(void), typeof(int));
        b.AppendReturn();

        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;)V")]
    public JavaMethodBody writeUTF(JavaClass cls)
    {
        byte[] encode = cls
            .PushConstant(
                new NameDescriptorClass(nameof(encodeUTF), "(Ljava/lang/String;)[B", typeof(DataOutputStream))).Split();
        byte[] writeShort =
            cls.PushConstant(new NameDescriptorClass(nameof(this.writeShort), "(I)V", typeof(DataOutputStream)))
                .Split();
        byte[] writeBuf = cls.PushConstant(new NameDescriptorClass("write", "([B)V", typeof(DataOutputStream))).Split();
        return new JavaMethodBody(4, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.invokestatic, encode),
                // this, arr
                new(JavaOpcode.dup),
                // this, arr, arr
                new(JavaOpcode.aload_0),
                new(JavaOpcode.swap),
                // this, arr, this, arr
                new(JavaOpcode.arraylength),
                new(JavaOpcode.invokevirtual, writeShort),
                // this, arr
                new(JavaOpcode.invokevirtual, writeBuf),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("(Ljava/lang/String;)[B")]
    public static Reference encodeUTF(Reference r)
    {
        var data = Jvm.ResolveString(r).EncodeJavaUnicode().ConvertToSigned();
        return Jvm.WrapPrimitiveArray(data);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody flush(JavaClass cls)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, cls.PushConstant(new NameDescriptor("flush", "()V")).Split()),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody close(JavaClass cls)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                // this.flush()
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual, cls.PushConstant(new NameDescriptor("flush", "()V")).Split()),
                // out.close()
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, cls.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, cls.PushConstant(new NameDescriptor("close", "()V")).Split()),
                new(JavaOpcode.@return),
            }
        };
    }
}
