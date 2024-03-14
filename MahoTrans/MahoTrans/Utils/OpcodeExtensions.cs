// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Compiler;
using MahoTrans.Runtime;

namespace MahoTrans.Utils;

public static class OpcodeExtensions
{
    /// <summary>
    ///     Returns true if execution of this opcode will always perform jump.
    /// </summary>
    /// <param name="opcode">Opcode to check.</param>
    public static bool IsJumpOpcode(this JavaOpcode opcode)
    {
        switch (opcode)
        {
            case JavaOpcode.@goto:
            case JavaOpcode.jsr:
            case JavaOpcode.ret:
            case JavaOpcode.tableswitch:
            case JavaOpcode.lookupswitch:
            case JavaOpcode.ireturn:
            case JavaOpcode.lreturn:
            case JavaOpcode.freturn:
            case JavaOpcode.dreturn:
            case JavaOpcode.areturn:
            case JavaOpcode.@return:
            case JavaOpcode.athrow:
            case JavaOpcode.goto_w:
            case JavaOpcode.jsr_w:
            case JavaOpcode._inplacereturn:
                return true;

            default:
                return false;
        }
    }

    public static OpcodeType GetOpcodeType(this MTOpcode opcode)
    {
        return opcode switch
        {
            MTOpcode.nop => OpcodeType.NoOp,
            MTOpcode.aconst_0 => OpcodeType.Constant,
            MTOpcode.iconst_m1 => OpcodeType.Constant,
            MTOpcode.iconst_0 => OpcodeType.Constant,
            MTOpcode.iconst_1 => OpcodeType.Constant,
            MTOpcode.iconst_2 => OpcodeType.Constant,
            MTOpcode.iconst_3 => OpcodeType.Constant,
            MTOpcode.iconst_4 => OpcodeType.Constant,
            MTOpcode.iconst_5 => OpcodeType.Constant,
            MTOpcode.lconst_0 => OpcodeType.Constant,
            MTOpcode.lconst_1 => OpcodeType.Constant,
            MTOpcode.lconst_2 => OpcodeType.Constant,
            MTOpcode.fconst_0 => OpcodeType.Constant,
            MTOpcode.fconst_1 => OpcodeType.Constant,
            MTOpcode.fconst_2 => OpcodeType.Constant,
            MTOpcode.dconst_0 => OpcodeType.Constant,
            MTOpcode.dconst_1 => OpcodeType.Constant,
            MTOpcode.dconst_2 => OpcodeType.Constant,
            MTOpcode.iconst => OpcodeType.Constant,
            MTOpcode.fconst => OpcodeType.Constant,
            MTOpcode.strconst => OpcodeType.Constant,
            MTOpcode.lconst => OpcodeType.Constant,
            MTOpcode.dconst => OpcodeType.Constant,
            MTOpcode.load => OpcodeType.Local,
            MTOpcode.store => OpcodeType.Local,
            MTOpcode.iinc => OpcodeType.Local,
            MTOpcode.iaload => OpcodeType.Array,
            MTOpcode.laload => OpcodeType.Array,
            MTOpcode.faload => OpcodeType.Array,
            MTOpcode.daload => OpcodeType.Array,
            MTOpcode.aaload => OpcodeType.Array,
            MTOpcode.baload => OpcodeType.Array,
            MTOpcode.caload => OpcodeType.Array,
            MTOpcode.saload => OpcodeType.Array,
            MTOpcode.iastore => OpcodeType.Array,
            MTOpcode.lastore => OpcodeType.Array,
            MTOpcode.fastore => OpcodeType.Array,
            MTOpcode.dastore => OpcodeType.Array,
            MTOpcode.aastore => OpcodeType.Array,
            MTOpcode.bastore => OpcodeType.Array,
            MTOpcode.castore => OpcodeType.Array,
            MTOpcode.sastore => OpcodeType.Array,
            MTOpcode.array_length => OpcodeType.Array,
            MTOpcode.pop => OpcodeType.Stack,
            MTOpcode.pop2 => OpcodeType.Stack,
            MTOpcode.swap => OpcodeType.Stack,
            MTOpcode.dup => OpcodeType.Stack,
            MTOpcode.dup2 => OpcodeType.Stack,
            MTOpcode.dup_x1 => OpcodeType.Stack,
            MTOpcode.dup_x2 => OpcodeType.Stack,
            MTOpcode.dup2_x1 => OpcodeType.Stack,
            MTOpcode.iadd => OpcodeType.Math,
            MTOpcode.ladd => OpcodeType.Math,
            MTOpcode.fadd => OpcodeType.Math,
            MTOpcode.dadd => OpcodeType.Math,
            MTOpcode.isub => OpcodeType.Math,
            MTOpcode.lsub => OpcodeType.Math,
            MTOpcode.fsub => OpcodeType.Math,
            MTOpcode.dsub => OpcodeType.Math,
            MTOpcode.imul => OpcodeType.Math,
            MTOpcode.lmul => OpcodeType.Math,
            MTOpcode.fmul => OpcodeType.Math,
            MTOpcode.dmul => OpcodeType.Math,
            MTOpcode.idiv => OpcodeType.Math,
            MTOpcode.ldiv => OpcodeType.Math,
            MTOpcode.fdiv => OpcodeType.Math,
            MTOpcode.ddiv => OpcodeType.Math,
            MTOpcode.irem => OpcodeType.Math,
            MTOpcode.lrem => OpcodeType.Math,
            MTOpcode.frem => OpcodeType.Math,
            MTOpcode.drem => OpcodeType.Math,
            MTOpcode.ineg => OpcodeType.Math,
            MTOpcode.lneg => OpcodeType.Math,
            MTOpcode.fneg => OpcodeType.Math,
            MTOpcode.dneg => OpcodeType.Math,
            MTOpcode.ishl => OpcodeType.Math,
            MTOpcode.lshl => OpcodeType.Math,
            MTOpcode.ishr => OpcodeType.Math,
            MTOpcode.lshr => OpcodeType.Math,
            MTOpcode.iushr => OpcodeType.Math,
            MTOpcode.lushr => OpcodeType.Math,
            MTOpcode.iand => OpcodeType.Math,
            MTOpcode.land => OpcodeType.Math,
            MTOpcode.ior => OpcodeType.Math,
            MTOpcode.lor => OpcodeType.Math,
            MTOpcode.ixor => OpcodeType.Math,
            MTOpcode.lxor => OpcodeType.Math,
            MTOpcode.i2l => OpcodeType.Conversion,
            MTOpcode.i2f => OpcodeType.Conversion,
            MTOpcode.i2d => OpcodeType.Conversion,
            MTOpcode.l2i => OpcodeType.Conversion,
            MTOpcode.l2f => OpcodeType.Conversion,
            MTOpcode.l2d => OpcodeType.Conversion,
            MTOpcode.f2i => OpcodeType.Conversion,
            MTOpcode.f2l => OpcodeType.Conversion,
            MTOpcode.f2d => OpcodeType.Conversion,
            MTOpcode.d2i => OpcodeType.Conversion,
            MTOpcode.d2l => OpcodeType.Conversion,
            MTOpcode.d2f => OpcodeType.Conversion,
            MTOpcode.i2b => OpcodeType.Conversion,
            MTOpcode.i2c => OpcodeType.Conversion,
            MTOpcode.i2s => OpcodeType.Conversion,
            MTOpcode.lcmp => OpcodeType.Compare,
            MTOpcode.fcmpl => OpcodeType.Compare,
            MTOpcode.fcmpg => OpcodeType.Compare,
            MTOpcode.dcmpl => OpcodeType.Compare,
            MTOpcode.dcmpg => OpcodeType.Compare,
            MTOpcode.ifeq => OpcodeType.Branch,
            MTOpcode.ifne => OpcodeType.Branch,
            MTOpcode.iflt => OpcodeType.Branch,
            MTOpcode.ifge => OpcodeType.Branch,
            MTOpcode.ifgt => OpcodeType.Branch,
            MTOpcode.ifle => OpcodeType.Branch,
            MTOpcode.if_cmpeq => OpcodeType.Branch,
            MTOpcode.if_cmpne => OpcodeType.Branch,
            MTOpcode.if_cmplt => OpcodeType.Branch,
            MTOpcode.if_cmpge => OpcodeType.Branch,
            MTOpcode.if_cmpgt => OpcodeType.Branch,
            MTOpcode.if_cmple => OpcodeType.Branch,
            MTOpcode.tableswitch => OpcodeType.Branch,
            MTOpcode.lookupswitch => OpcodeType.Branch,
            MTOpcode.jump => OpcodeType.Jump,
            MTOpcode.return_value => OpcodeType.Return,
            MTOpcode.return_void => OpcodeType.Return,
            MTOpcode.return_void_inplace => OpcodeType.Return,
            MTOpcode.athrow => OpcodeType.Throw,
            MTOpcode.invoke_virtual => OpcodeType.VirtCall,
            MTOpcode.invoke_static => OpcodeType.Call,
            MTOpcode.invoke_instance => OpcodeType.Call,
            MTOpcode.invoke_virtual_void_no_args_bysig => OpcodeType.VirtCall,
            MTOpcode.get_static => OpcodeType.Static,
            MTOpcode.set_static => OpcodeType.Static,
            MTOpcode.get_static_init => OpcodeType.Initializer,
            MTOpcode.set_static_init => OpcodeType.Initializer,
            MTOpcode.new_obj => OpcodeType.Alloc,
            MTOpcode.new_prim_arr => OpcodeType.Alloc,
            MTOpcode.new_arr => OpcodeType.Alloc,
            MTOpcode.new_multi_arr => OpcodeType.Alloc,
            MTOpcode.monitor_enter => OpcodeType.Monitor,
            MTOpcode.monitor_exit => OpcodeType.Monitor,
            MTOpcode.checkcast => OpcodeType.Cast,
            MTOpcode.instanceof => OpcodeType.Cast,
            MTOpcode.bridge => OpcodeType.Bridge,
            MTOpcode.bridge_init => OpcodeType.Initializer,
            MTOpcode.error_no_class => OpcodeType.Error,
            MTOpcode.error_no_field => OpcodeType.Error,
            MTOpcode.error_no_method => OpcodeType.Error,
            MTOpcode.error_bytecode => OpcodeType.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(opcode), opcode, null)
        };
    }

    /// <summary>
    /// Checks total count of opcodes, used in all methods in all classes loaded in the jvm.
    /// </summary>
    /// <param name="jvm">Jvm to check.</param>
    /// <param name="opcode">Opcode to count.</param>
    /// <returns>Total count of opcode usages.</returns>
    public static int CountOpcodes(this JvmState jvm, MTOpcode opcode)
    {
        int count = 0;
        foreach (var cls in jvm.Classes.Values)
        {
            foreach (var m in cls.Methods.Values)
            {
                if (m.JavaBody != null)
                {
                    foreach (var instr in m.JavaBody.LinkedCode)
                    {
                        if (instr.Opcode == opcode)
                            count++;
                    }
                }
            }
        }

        return count;
    }
}
