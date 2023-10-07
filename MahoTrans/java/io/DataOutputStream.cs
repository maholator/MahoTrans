using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.io;

public class DataOutputStream : OutputStream
{
    [JavaType(typeof(OutputStream))] public Reference @out;

    [JavaIgnore] private readonly NameDescriptorClass _streamDescriptor =
        new("out", typeof(OutputStream), typeof(DataOutputStream));

    [JavaIgnore]
    private readonly NameDescriptorClass _write = new NameDescriptorClass("write", "(I)V", typeof(OutputStream));

    [InitMethod]
    public void Init([JavaType(typeof(OutputStream))] Reference r)
    {
        @out = r;
    }

    /// <summary>
    /// Writes one byte into <see cref="@out"/>. Do all operations via this method.
    /// </summary>
    /// <param name="class"></param>
    /// <returns></returns>
    [JavaDescriptor("(I)V")]
    public JavaMethodBody write___byte(JavaClass @class)
    {
        byte[] streamWrite = @class.PushConstant(_write).Split();
        return new JavaMethodBody(2, 2)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.getfield, @class.PushConstant(_streamDescriptor).Split()),
                new(JavaOpcode.iload_1),
                new(JavaOpcode.invokevirtual, streamWrite),
                new(JavaOpcode.@return),
            }
        };
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