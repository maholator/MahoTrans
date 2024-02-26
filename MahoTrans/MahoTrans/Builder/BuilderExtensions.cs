// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
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

    public static void AppendPop(this JavaMethodBuilder b) => b.Append(JavaOpcode.pop);

    public static void AppendPop2(this JavaMethodBuilder b) => b.Append(JavaOpcode.pop2);

    public static void AppendDup(this JavaMethodBuilder b) => b.Append(JavaOpcode.dup);

    public static void AppendDup2(this JavaMethodBuilder b) => b.Append(JavaOpcode.dup2);

    /// <summary>
    ///     Emits <see cref="JavaOpcode.sipush" /> instruction with given args.
    /// </summary>
    /// <param name="b">Builder.</param>
    /// <param name="value">Short to push.</param>
    public static void AppendShort(this JavaMethodBuilder b, short value) =>
        b.Append(new Instruction(JavaOpcode.sipush, value.Split()));

    /// <summary>
    ///     Emits <see cref="JavaOpcode.sipush" /> instruction with given args.
    /// </summary>
    /// <param name="b">Builder.</param>
    /// <param name="value">Character to push.</param>
    public static void AppendChar(this JavaMethodBuilder b, char value) =>
        b.Append(new Instruction(JavaOpcode.sipush, ((short)value).Split()));

    /// <summary>
    ///     Emits <see cref="JavaOpcode.bipush" /> instruction with given args.
    /// </summary>
    /// <param name="b">Builder.</param>
    /// <param name="value">Sbyte to push.</param>
    public static void AppendSbyte(this JavaMethodBuilder b, sbyte value) => b.Append(new Instruction(
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

    public static void Append(this JavaMethodBuilder b, params Instruction[] code)
    {
        foreach (var instruction in code)
        {
            b.Append(instruction);
        }
    }

    public static void Append(this JavaMethodBuilder b, params JavaOpcode[] opcodes)
    {
        foreach (var opcode in opcodes)
        {
            b.Append(opcode);
        }
    }
}