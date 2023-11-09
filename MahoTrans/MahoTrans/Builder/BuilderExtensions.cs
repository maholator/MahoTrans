using MahoTrans.Runtime;

namespace MahoTrans.Builder;

public static class BuilderExtensions
{
    public static void AppendThis(this JavaMethodBuilder b) => b.Append(JavaOpcode.aload_0);

    public static void AppendInc(this JavaMethodBuilder b, byte variable, sbyte value)
    {
        b.Append(new Instruction(JavaOpcode.iinc, new[] { variable, (byte)value }));
    }
}