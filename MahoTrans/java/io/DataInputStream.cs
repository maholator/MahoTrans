using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.io;

public class DataInputStream : InputStream
{
    [JavaType(typeof(InputStream))] public Reference @in;

    [JavaIgnore] private readonly NameDescriptorClass _streamDescriptor =
        new("in", "Ljava/io/InputStream;", "java/io/DataInputStream");

    [JavaIgnore] private const string input_stream = "java/io/InputStream";

    [InitMethod]
    public void Init([JavaType(typeof(InputStream))] Reference r)
    {
        @in = r;
    }

    [JavaDescriptor("([BII)I")]
    public JavaMethodBody read(JavaClass @class)
    {
        return new JavaMethodBody(4, 4)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.aload_1),
                new(JavaOpcode.iload_2),
                new(JavaOpcode.iload_3),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("read", "([BII)I", input_stream)).Split()),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()S")]
    public JavaMethodBody readShort(JavaClass @class)
    {
        //TODO check stream end
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
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
        //TODO check stream end
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
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

    [JavaDescriptor("()I")]
    public JavaMethodBody readInt(JavaClass @class)
    {
        //TODO check stream end
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(5, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
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

    [JavaDescriptor("()B")]
    public JavaMethodBody readByte(JavaClass @class)
    {
        //TODO check stream end
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody readUnsignedByte(JavaClass @class)
    {
        //TODO check stream end
        byte[] streamRead = @class.PushConstant(new NameDescriptorClass("read", "()I", input_stream)).Split();
        return new JavaMethodBody(3, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.invokevirtual, streamRead),
                new(JavaOpcode.ireturn)
            }
        };
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody readUTF(JavaClass @class)
    {
        //TODO check stream end
        byte[] unsignedRead = @class
            .PushConstant(new NameDescriptorClass(nameof(readUnsignedShort), "()I", "java/io/DataInputStream")).Split();
        return new JavaMethodBody(4, 3)
        {
            // locals: this, count, buffer
            RawCode = new Instruction[]
            {
                // stack: (nothing)
                new(JavaOpcode.aload_0),
                // stack: this
                new(JavaOpcode.dup),
                // stack: this, this
                new(JavaOpcode.invokevirtual, unsignedRead),
                // stack: this, count
                new(JavaOpcode.dup),
                new(JavaOpcode.istore_1),
                // stack: this, count.
                new(JavaOpcode.newarray, new[] { (byte)ArrayType.T_BYTE }),
                // stack: this, arr.
                new(JavaOpcode.dup),
                new(JavaOpcode.astore_2),
                // stack: this, arr.
                new(JavaOpcode.iconst_0),
                new(JavaOpcode.iload_1),
                // stack: this, arr, from, len.
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("read", "([BII)I", input_stream)).Split()),
                // stack: read count
                new(JavaOpcode.pop), //TODO check stream end
                // stack: (nothing)
                new(JavaOpcode.aload_0),
                new(JavaOpcode.aload_2),
                // stack: this, arr
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass(nameof(decodeUTF), "([B)Ljava/lang/String;",
                        input_stream)).Split()),
                new(JavaOpcode.areturn)
            }
        };
    }

    [JavaDescriptor("([B)Ljava/lang/String;")]
    public static Reference decodeUTF(Reference r)
    {
        var arr = Heap.ResolveArray<sbyte>(r);
        var str = arr.ToUnsigned().DecodeJavaUnicode();
        return Heap.AllocateString(str);
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody close(JavaClass @class)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("close", "()V", input_stream)).Split()),
                new(JavaOpcode.@return)
            }
        };
    }

    [JavaDescriptor("(J)J")]
    public JavaMethodBody skip(JavaClass @class)
    {
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.lload_1),
                new(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("skip", "(J)J", input_stream)).Split()),
                new(JavaOpcode.lreturn)
            }
        };
    }
}