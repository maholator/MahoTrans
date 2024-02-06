// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.io;

public class DataInputStream : InputStream, DataInput
{
    [JavaType(typeof(InputStream))] public Reference @in;

    private static readonly NameDescriptorClass StreamDescriptor =
        new("in", "Ljava/io/InputStream;", dis);

    private static readonly NameDescriptorClass ReadByteNdc = new(nameof(readUnsignedByte), "()I", dis);

    [JavaIgnore] private const string dis = "java/io/DataInputStream";

    [JavaIgnore] private const string input_stream = "java/io/InputStream";

    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference r)
    {
        @in = r;
    }

    [JavaDescriptor("([BII)I")]
    public JavaMethodBody read___buf(JavaClass @class)
    {
        return new JavaMethodBody(4, 4)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.iload_2),
                new(JavaOpcode.iload_3),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("read", "([BII)I", input_stream)).Split()),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("([B)I")]
    public JavaMethodBody read___bufFull(JavaClass @class)
    {
        return new JavaMethodBody(4, 4)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.iconst_0),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.arraylength),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("read", "([BII)I", input_stream)).Split()),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("([B)V")]
    public JavaMethodBody readFully(JavaClass cls)
    {
        byte[] streamRead = cls.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(3, 4)
        {
            // locals: this, arr, i, len
            RawCode = new Instruction[]
            {
                new(JavaOpcode.iconst_0),
                new(JavaOpcode.istore_2),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.arraylength),
                new(JavaOpcode.istore_3),
                new(JavaOpcode.@goto, 14.Split()),
                // loop begin
                new(JavaOpcode.aload_1),
                new(JavaOpcode.iload_2),
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.i2b),
                new(JavaOpcode.bastore),
                new(JavaOpcode.iinc, new byte[] { 2, 1 }),
                // loop end
                new(JavaOpcode.iload_2),
                new(JavaOpcode.iload_3),
                new(JavaOpcode.if_icmplt, (-13).Split()),
                new(JavaOpcode.@return),
            }
        };
    }

    [JavaDescriptor("([BII)V")]
    public JavaMethodBody readFully___bounds(JavaClass cls)
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
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload, 4);
            b.AppendThis();
            b.AppendVirtcall(nameof(readByte), typeof(sbyte));
            b.Append(JavaOpcode.bastore);

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

    [JavaDescriptor("()S")]
    public JavaMethodBody readShort(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.ishl),
                new(JavaOpcode.istore_1),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.ior),
                new(JavaOpcode.i2s),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody readUnsignedShort(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.ishl),
                new(JavaOpcode.istore_1),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.ior),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()C")]
    public JavaMethodBody readChar(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass(nameof(readUnsignedShort), "()I", dis)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.i2c),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody readInt(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(5, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.dup),
                new(JavaOpcode.dup),
                new(JavaOpcode.dup),

                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.bipush, new byte[] { 24 }),
                new(JavaOpcode.ishl),
                new(JavaOpcode.istore_1),

                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.bipush, new byte[] { 16 }),
                new(JavaOpcode.ishl),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.ior),
                new(JavaOpcode.istore_1),

                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.bipush, new byte[] { 8 }),
                new(JavaOpcode.ishl),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.ior),
                new(JavaOpcode.istore_1),

                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.ior),

                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()J")]
    public JavaMethodBody readLong(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.dup);
        b.AppendVirtcall(nameof(readInt), typeof(int));
        b.Append(JavaOpcode.i2l);
        b.Append(JavaOpcode.bipush, 32);
        b.Append(JavaOpcode.lshl);
        b.Append(JavaOpcode.lstore_1);
        b.AppendVirtcall(nameof(readInt), typeof(int));
        b.Append(JavaOpcode.i2l);
        b.Append(JavaOpcode.lload_1);
        b.Append(JavaOpcode.lor);
        b.Append(JavaOpcode.lreturn);
        return b.Build(3, 2);
    }

    [JavaDescriptor("()B")]
    public JavaMethodBody readByte(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.i2b),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()Z")]
    public JavaMethodBody readBoolean(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(ReadByteNdc).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()F")]
    public JavaMethodBody readFloat(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall(nameof(readInt), typeof(int));
        b.AppendStaticCall<Float>(nameof(Float.intBitsToFloat), typeof(float), typeof(int));
        b.AppendReturnFloat();
        return b.Build(1, 1);
    }

    /// <summary>
    ///     Reads one byte from <see cref="@in" />. Performs EOF check. Always read using this method.
    /// </summary>
    /// <param name="class"></param>
    /// <returns></returns>
    [JavaDescriptor("()I")]
    public JavaMethodBody readUnsignedByte(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.dup),
                new(JavaOpcode.iflt, 4.Split()),
                new(JavaOpcode.ireturn),
                new(JavaOpcode.pop),
                new(JavaOpcode.newobject, @class.PushConstant("java/io/EOFException").Split()),
                new(JavaOpcode.dup),
                new(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("<init>", "()V", "java/io/EOFException")).Split()),
                new(JavaOpcode.athrow)
            }
        };
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody read___one(JavaClass @class)
    {
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.ireturn),
            }
        };
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody readUTF(JavaClass @class)
    {
        //TODO check stream end
        byte[] unsignedRead = @class
            .PushConstant(new NameDescriptorClass(nameof(readUnsignedShort), "()I", dis)).Split();
        return new JavaMethodBody(4, 2)
        {
            // locals: this, buffer
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                // stack: this
                new(JavaOpcode.dup),
                // stack: this, this
                new(JavaOpcode.invokevirtual, unsignedRead),
                // stack: this, count
                new(JavaOpcode.newarray, new[] { (byte)ArrayType.T_BYTE }),
                // stack: this, arr.
                new(JavaOpcode.dup),
                new(JavaOpcode.astore_1),
                // stack: this, arr.
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass(nameof(readFully), "([B)V", dis)).Split()),
                // stack: -
                new(JavaOpcode.aload_1),
                // stack: arr
                new(JavaOpcode.invokestatic,
                    @class.PushConstant(new NameDescriptorClass(nameof(decodeUTF), "([B)Ljava/lang/String;",
                        dis)).Split()),
                new(JavaOpcode.areturn)
            }
        };
    }

    [JavaDescriptor("([B)Ljava/lang/String;")]
    public static Reference decodeUTF(Reference r)
    {
        var arr = Jvm.ResolveArray<sbyte>(r);
        var str = arr.ToUnsigned().DecodeJavaUnicode();
        return Jvm.AllocateString(str);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody close(JavaClass @class)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("close", "()V", input_stream)).Split()),
                new(JavaOpcode.@return)
            }
        };
    }

    [JavaDescriptor("(J)J")]
    public new JavaMethodBody skip(JavaClass @class)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("skip", "(J)J", input_stream)).Split()),
                new(JavaOpcode.lreturn)
            }
        };
    }

    [JavaDescriptor("(I)I")]
    public JavaMethodBody skipBytes(JavaClass @class)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.i2l),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("skip", "(J)J", input_stream)).Split()),
                new(JavaOpcode.l2i),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody available(JavaClass @class)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(StreamDescriptor).Split()),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass(nameof(available), "()I", input_stream)).Split()),
                new(JavaOpcode.i2l),
                new(JavaOpcode.lreturn)
            }
        };
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody reset(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(@in), typeof(InputStream));
        b.AppendVirtcall(nameof(reset), typeof(void));
        b.AppendReturn();
        return b.Build(1, 1);
    }

    [JavaDescriptor("(I)V")]
    public JavaMethodBody mark(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(@in), typeof(InputStream));
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall(nameof(mark), typeof(void));
        b.AppendReturn();
        return b.Build(2, 2);
    }

    [JavaDescriptor("()Z")]
    public JavaMethodBody markSupported(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetLocalField(nameof(@in), typeof(InputStream));
        b.AppendVirtcall(nameof(markSupported), typeof(bool));
        b.AppendReturnInt();
        return b.Build(2, 2);
    }
}