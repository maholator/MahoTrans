// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection.Emit;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    public void PushConstant(LinkedInstruction instr)
    {
        using (new MarshallerWrapper(this, ^1))
        {
            switch (instr.Opcode)
            {
                case MTOpcode.iconst_m1:
                    _il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case MTOpcode.iconst_0:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case MTOpcode.iconst_1:
                    _il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case MTOpcode.iconst_2:
                    _il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case MTOpcode.iconst_3:
                    _il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case MTOpcode.iconst_4:
                    _il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case MTOpcode.iconst_5:
                    _il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case MTOpcode.lconst_0:
                    _il.Emit(OpCodes.Ldc_I8, 0L);
                    break;
                case MTOpcode.lconst_1:
                    _il.Emit(OpCodes.Ldc_I8, 1L);
                    break;
                case MTOpcode.lconst_2:
                    _il.Emit(OpCodes.Ldc_I8, 2L);
                    break;
                case MTOpcode.fconst_0:
                    _il.Emit(OpCodes.Ldc_R4, 0F);
                    break;
                case MTOpcode.fconst_1:
                    _il.Emit(OpCodes.Ldc_R4, 1F);
                    break;
                case MTOpcode.fconst_2:
                    _il.Emit(OpCodes.Ldc_R4, 2F);
                    break;
                case MTOpcode.dconst_0:
                    _il.Emit(OpCodes.Ldc_R8, 0D);
                    break;
                case MTOpcode.dconst_1:
                    _il.Emit(OpCodes.Ldc_R8, 1D);
                    break;
                case MTOpcode.dconst_2:
                    _il.Emit(OpCodes.Ldc_R8, 2D);
                    break;
                case MTOpcode.iconst:
                    _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                    break;
                case MTOpcode.strconst:
                    _il.Emit(OpCodes.Ldstr, (string)instr.Data);
                    break;
                case MTOpcode.lconst:
                    _il.Emit(OpCodes.Ldc_I8, (long)instr.Data);
                    break;
                case MTOpcode.dconst:
                    _il.Emit(OpCodes.Ldc_R8, (double)instr.Data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}