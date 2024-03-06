// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    private void CrossConstant(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Constant);
        using (BeginMarshalSection(^1))
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

    private void CrossLocal(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Local);
        switch (instr.Opcode)
        {
            case MTOpcode.load:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                    _il.Emit(OpCodes.Call, CompilerUtils.LocalGetMethods[(PrimitiveType)instr.ShortData]);
                }

                break;
            case MTOpcode.store:
            {
                _il.BeginScope();
                // catching value:
                var temp = _il.DeclareLocal(StackTypes[_instrIndex][^1].ToType());
                _il.Emit(OpCodes.Stloc, temp);

                // frame > value > index
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldloc, temp);
                _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                _il.Emit(OpCodes.Call, CompilerUtils.LocalSetMethods[(PrimitiveType)instr.ShortData]);

                _il.EndScope();
                break;
            }
            case MTOpcode.iinc:
                // frame object:
                _il.Emit(OpCodes.Ldarg_0);
                // index goes first.
                // ReSharper disable once RedundantCast
                _il.Emit(OpCodes.Ldc_I4, (int)instr.ShortData);
                // now the value.
                _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                _il.Emit(OpCodes.Call, CompilerUtils.LocalIncrement);
                // this is no-op for stack.
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CrossArray(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Array);
        switch (instr.Opcode)
        {
            case MTOpcode.iaload:
            case MTOpcode.laload:
            case MTOpcode.faload:
            case MTOpcode.daload:
            case MTOpcode.aaload:
            case MTOpcode.baload:
            case MTOpcode.caload:
            case MTOpcode.saload:
                // take value via helper.
                throw new NotImplementedException();
            case MTOpcode.iastore:
            case MTOpcode.lastore:
            case MTOpcode.fastore:
            case MTOpcode.dastore:
            case MTOpcode.aastore:
            case MTOpcode.bastore:
            case MTOpcode.castore:
            case MTOpcode.sastore:
                // convert int to short/byte if needed, set via helper.
                throw new NotImplementedException();
            case MTOpcode.array_length:
                using (BeginMarshalSection(^1))
                {
                    //TODO this won't work due to marshaller
                    _il.Emit(OpCodes.Call, CompilerUtils.ArrayLength);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CrossStack(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Stack);
        switch (instr.Opcode)
        {
            case MTOpcode.pop:
                _il.Emit(OpCodes.Pop);
                break;
            case MTOpcode.pop2:
                _il.Emit(OpCodes.Pop);
                _il.Emit(OpCodes.Pop);
                break;
            case MTOpcode.swap:
            {
                _il.BeginScope();
                var left = _il.DeclareLocal(StackTypes[_instrIndex][^2].ToType());
                var right = _il.DeclareLocal(StackTypes[_instrIndex][^1].ToType());

                // left > right
                _il.Emit(OpCodes.Stloc, right);
                _il.Emit(OpCodes.Stloc, left);

                using (BeginMarshalSection(^2))
                {
                    _il.Emit(OpCodes.Ldloc, right);
                }

                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Ldloc, left);
                }

                // right > left
                _il.EndScope();
                break;
            }
            case MTOpcode.dup:
            {
                _il.BeginScope();
                var temp = _il.DeclareLocal(StackTypes[_instrIndex][^1].ToType());
                _il.Emit(OpCodes.Stloc, temp);
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Ldloc, temp);
                }

                _il.EndScope();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}