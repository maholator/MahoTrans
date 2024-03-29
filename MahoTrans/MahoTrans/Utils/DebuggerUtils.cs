// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using JetBrains.Annotations;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Array = java.lang.Array;
using String = java.lang.String;

namespace MahoTrans.Utils;

/// <summary>
///     Various tools for debuggers.
/// </summary>
[PublicAPI]
public static class DebuggerUtils
{
    /// <summary>
    ///     Returns how many values are popped from stack by this instruction.
    /// </summary>
    /// <param name="instruction">Instruction to check.</param>
    /// <returns>Values count.</returns>
    public static int GetPoppedValuesCount(this LinkedInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case MTOpcode.nop:
                return 0;

            case MTOpcode.aconst_0:
            case MTOpcode.iconst_m1:
            case MTOpcode.iconst_0:
            case MTOpcode.iconst_1:
            case MTOpcode.iconst_2:
            case MTOpcode.iconst_3:
            case MTOpcode.iconst_4:
            case MTOpcode.iconst_5:
            case MTOpcode.lconst_0:
            case MTOpcode.lconst_1:
            case MTOpcode.lconst_2:
            case MTOpcode.fconst_0:
            case MTOpcode.fconst_1:
            case MTOpcode.fconst_2:
            case MTOpcode.dconst_0:
            case MTOpcode.dconst_1:
            case MTOpcode.dconst_2:
            case MTOpcode.iconst:
            case MTOpcode.fconst:
            case MTOpcode.strconst:
            case MTOpcode.lconst:
            case MTOpcode.dconst:
                // consts push 1 value and take nothing.
                return 0;

            case MTOpcode.load:
                // loads load 1 value and take nothing.
                return 0;

            case MTOpcode.store:
                // stores take 1 value.
                return 1;

            case MTOpcode.iinc:
                return 0;

            case MTOpcode.iaload:
            case MTOpcode.laload:
            case MTOpcode.faload:
            case MTOpcode.daload:
            case MTOpcode.aaload:
            case MTOpcode.baload:
            case MTOpcode.caload:
            case MTOpcode.saload:
                // aloads take array and index - 2.
                return 2;

            case MTOpcode.iastore:
            case MTOpcode.lastore:
            case MTOpcode.fastore:
            case MTOpcode.dastore:
            case MTOpcode.aastore:
            case MTOpcode.bastore:
            case MTOpcode.castore:
            case MTOpcode.sastore:
                //astores take array, index and value - 3.
                return 3;

            case MTOpcode.array_length:
                return 1;

            case MTOpcode.pop:
                return 1;

            case MTOpcode.pop2:
                return 2;

            case MTOpcode.swap:
                return 2;

            case MTOpcode.dup:
                return 1;

            case MTOpcode.dup2:
                return 2;

            case MTOpcode.dup_x1:
                return 2;

            case MTOpcode.dup_x2:
                return 3;

            case MTOpcode.dup2_x1:
                return 3;

            case MTOpcode.iadd:
            case MTOpcode.ladd:
            case MTOpcode.fadd:
            case MTOpcode.dadd:
            case MTOpcode.isub:
            case MTOpcode.lsub:
            case MTOpcode.fsub:
            case MTOpcode.dsub:
            case MTOpcode.imul:
            case MTOpcode.lmul:
            case MTOpcode.fmul:
            case MTOpcode.dmul:
            case MTOpcode.idiv:
            case MTOpcode.ldiv:
            case MTOpcode.fdiv:
            case MTOpcode.ddiv:
            case MTOpcode.irem:
            case MTOpcode.lrem:
            case MTOpcode.frem:
            case MTOpcode.drem:
                // math do pop+pop=push.
                return 2;

            case MTOpcode.ineg:
            case MTOpcode.lneg:
            case MTOpcode.fneg:
            case MTOpcode.dneg:
                return 1;

            case MTOpcode.ishl:
            case MTOpcode.lshl:
            case MTOpcode.ishr:
            case MTOpcode.lshr:
            case MTOpcode.iushr:
            case MTOpcode.lushr:
                return 2;

            case MTOpcode.iand:
            case MTOpcode.land:
            case MTOpcode.ior:
            case MTOpcode.lor:
            case MTOpcode.ixor:
            case MTOpcode.lxor:
                return 2;

            case MTOpcode.i2l:
            case MTOpcode.i2f:
            case MTOpcode.i2d:
            case MTOpcode.l2i:
            case MTOpcode.l2f:
            case MTOpcode.l2d:
            case MTOpcode.f2i:
            case MTOpcode.f2l:
            case MTOpcode.f2d:
            case MTOpcode.d2i:
            case MTOpcode.d2l:
            case MTOpcode.d2f:
            case MTOpcode.i2b:
            case MTOpcode.i2c:
            case MTOpcode.i2s:
                // pop -> conversion -> push
                return 1;

            case MTOpcode.lcmp:
            case MTOpcode.fcmpl:
            case MTOpcode.fcmpg:
            case MTOpcode.dcmpl:
            case MTOpcode.dcmpg:
                // pop?pop=push
                return 2;

            case MTOpcode.ifeq:
            case MTOpcode.ifne:
            case MTOpcode.iflt:
            case MTOpcode.ifge:
            case MTOpcode.ifgt:
            case MTOpcode.ifle:
                return 1;

            case MTOpcode.if_cmpeq:
            case MTOpcode.if_cmpne:
            case MTOpcode.if_cmplt:
            case MTOpcode.if_cmpge:
            case MTOpcode.if_cmpgt:
            case MTOpcode.if_cmple:
                return 2;

            case MTOpcode.tableswitch:
                return 1;

            case MTOpcode.lookupswitch:
                return 1;

            case MTOpcode.jump:
                return 0;

            case MTOpcode.return_value:
                return 1;

            case MTOpcode.return_void:
                return 0;

            case MTOpcode.return_void_inplace:
                return 0;

            case MTOpcode.athrow:
                return 1;

            case MTOpcode.invoke_static:
            case MTOpcode.invoke_static_simple:
                return ((Method)instruction.Data).ArgsCount;

            case MTOpcode.invoke_instance:
            case MTOpcode.invoke_instance_simple:
                return ((Method)instruction.Data).ArgsCount + 1;

            case MTOpcode.invoke_virtual:
                return instruction.ShortData + 1;

            case MTOpcode.invoke_virtual_void_no_args_bysig:
                return 3; // this, name, descriptor

            case MTOpcode.new_obj:
                return 0;

            case MTOpcode.new_prim_arr:
                return 1;

            case MTOpcode.new_arr:
                return 1;

            case MTOpcode.new_multi_arr:
                return instruction.IntData;

            case MTOpcode.monitor_enter:
                return 1;

            case MTOpcode.monitor_exit:
                return 1;

            case MTOpcode.checkcast:
                return 1;

            case MTOpcode.instanceof:
                return 1;

            case MTOpcode.bridge:
            case MTOpcode.bridge_init:
                return instruction.IntData;

            case MTOpcode.set_static:
            case MTOpcode.set_static_init:
                return 1;

            case MTOpcode.get_static:
            case MTOpcode.get_static_init:
                return 0;

            default:
                throw new ArgumentException($"Opcode {instruction.Opcode} is not supported.");
        }
    }

    /// <summary>
    ///     Pretty-prints value of a reference to show it in debugger.
    /// </summary>
    /// <param name="value">Pointer to print.</param>
    /// <param name="jvm">JVM.</param>
    /// <returns>String in format "pointer (description of reference value)".</returns>
    public static string PrettyPrintReference(Reference value, JvmState jvm)
    {
        if (value.IsNull)
            return "null reference";
        try
        {
            var obj = jvm.ResolveObject(value);
            switch (obj)
            {
                case String s:
                    return $"string \"{s.Value}\"";
                case Array a:
                {
                    var nativeArrayElementType = a.BaseArray.GetType().GetElementType()!.Name;
                    return $"{nativeArrayElementType}[{a.BaseArray.Length}] array, {obj.JavaClass}";
                }
                case Class cls:
                    return $"class \"{cls.InternalClass.Name}\"";
                default:
                    return $"object of {obj.JavaClass.Name}";
            }
        }
        catch
        {
            return "failed to evaluate";
        }
    }

    /// <summary>
    ///     Pretty-prints instruction to show it in debugger.
    /// </summary>
    /// <param name="method">Method that contains the instruction.</param>
    /// <param name="index">Number of the instruction.</param>
    /// <param name="jvm">JVM.</param>
    /// <returns>Opcode and additional info for it.</returns>
    public static string PrettyPrintInstruction(JavaMethodBody method, int index,
                                                JvmState jvm)
    {
        var consts = method.Method.Class.Constants;
        var rawOpcode = method.Code[index].Opcode;
        var args = method.Code[index].Args;
        var linked = method.LinkedCode[index];

        switch (rawOpcode)
        {
            case JavaOpcode.bipush:
                return $"{rawOpcode} {(sbyte)args[0]}";

            case JavaOpcode.sipush:
                return $"{rawOpcode} {args.Combine()}";

            case JavaOpcode.ldc:
                return $"{rawOpcode} {consts[args[0]]}";

            case JavaOpcode.ldc_w:
            case JavaOpcode.ldc2_w:
                return $"{rawOpcode} {consts[args.Combine()]}";

            case JavaOpcode.iload:
            case JavaOpcode.lload:
            case JavaOpcode.fload:
            case JavaOpcode.dload:
            case JavaOpcode.aload:
            case JavaOpcode.istore:
            case JavaOpcode.lstore:
            case JavaOpcode.fstore:
            case JavaOpcode.dstore:
            case JavaOpcode.astore:
                return $"{rawOpcode}_{args[0]}";

            case JavaOpcode.ifeq:
            case JavaOpcode.ifne:
            case JavaOpcode.iflt:
            case JavaOpcode.ifge:
            case JavaOpcode.ifgt:
            case JavaOpcode.ifle:
            case JavaOpcode.if_icmpeq:
            case JavaOpcode.if_icmpne:
            case JavaOpcode.if_icmplt:
            case JavaOpcode.if_icmpge:
            case JavaOpcode.if_icmpgt:
            case JavaOpcode.if_icmple:
            case JavaOpcode.if_acmpeq:
            case JavaOpcode.if_acmpne:
            case JavaOpcode.@goto:
            case JavaOpcode.ifnull:
            case JavaOpcode.ifnonnull:
                return $"{rawOpcode} (to {linked.IntData})";

            case JavaOpcode.putfield:
            case JavaOpcode.getfield:
            case JavaOpcode.putstatic:
            case JavaOpcode.getstatic:
            case JavaOpcode.invokeinterface:
            case JavaOpcode.invokevirtual:
            case JavaOpcode.invokespecial:
            case JavaOpcode.invokestatic:
            case JavaOpcode.newobject:
            case JavaOpcode.anewarray:
            case JavaOpcode.checkcast:
            case JavaOpcode.instanceof:
                return $"{rawOpcode} {method.UsedEntities[index]}";

            case JavaOpcode.newarray:
                return $"{rawOpcode} {(ArrayType)args[0]}";

            case JavaOpcode.wide:
                return $"{rawOpcode} {(JavaOpcode)args[0]}";

            case JavaOpcode.multianewarray:
                return $"{rawOpcode} {args[2]} {method.UsedEntities[index]}";

            default:
                return $"{rawOpcode}";
        }
    }
}
