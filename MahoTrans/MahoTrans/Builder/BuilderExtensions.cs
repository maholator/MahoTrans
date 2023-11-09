using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Builder;

public static class BuilderExtensions
{
    public static void AppendThis(this JavaMethodBuilder b) => b.Append(JavaOpcode.aload_0);

    public static void AppendInc(this JavaMethodBuilder b, byte variable, sbyte value)
    {
        b.Append(new Instruction(JavaOpcode.iinc, new[] { variable, (byte)value }));
    }

    public static void AppendConstant(this JavaMethodBuilder b, short value) =>
        b.Append(new Instruction(JavaOpcode.sipush, value.Split()));

    public static void AppendConstant(this JavaMethodBuilder b, char value) =>
        b.Append(new Instruction(JavaOpcode.sipush, ((short)value).Split()));

    public static void AppendConstant(this JavaMethodBuilder b, sbyte value) => b.Append(new Instruction(
        JavaOpcode.bipush, new[] { (byte)value }));

    public static void AppendReturn(this JavaMethodBuilder b) => b.Append(JavaOpcode.@return);
    public static void AppendReturnReference(this JavaMethodBuilder b) => b.Append(JavaOpcode.areturn);
    public static void AppendReturnInt(this JavaMethodBuilder b) => b.Append(JavaOpcode.ireturn);
}