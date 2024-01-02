// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

    public static void AppendReturnLong(this JavaMethodBuilder b) => b.Append(JavaOpcode.lreturn);

    public static void AppendReturnFloat(this JavaMethodBuilder b) => b.Append(JavaOpcode.freturn);

    public static void AppendReturnDouble(this JavaMethodBuilder b) => b.Append(JavaOpcode.dreturn);

    public static void AppendReturnNull(this JavaMethodBuilder b)
    {
        b.Append(JavaOpcode.aconst_null);
        b.AppendReturnReference();
    }
}