// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using static MahoTrans.Compiler.CompilerUtils;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    private void crossConstant(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Constant);
        using (BeginMarshalSection(^1))
        {
            switch (instr.Opcode)
            {
                case MTOpcode.aconst_0:
                    _il.Emit(OpCodes.Call, typeof(Reference).GetProperty(nameof(Reference.Null))!.GetMethod!);
                    break;
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
                case MTOpcode.fconst:
                    _il.Emit(OpCodes.Ldc_R4, BitConverter.Int32BitsToSingle(instr.IntData));
                    break;
                case MTOpcode.strconst:
                    _il.Emit(OpCodes.Ldsfld, Context);
                    _il.Emit(OpCodes.Ldstr, (string)instr.Data);
                    _il.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.InternalizeString))!);
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

    private void crossLocal(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Local);
        switch (instr.Opcode)
        {
            case MTOpcode.load:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                    _il.Emit(OpCodes.Call, LocalGetMethods[(PrimitiveType)instr.ShortData]);
                }

                break;
            case MTOpcode.store:
            {
                _il.BeginScope();
                // catching value:
                var temp = _il.DeclareLocal(StackTypes[LocalInstrIndex][^1].ToType());
                _il.Emit(OpCodes.Stloc, temp);

                // frame > value > index
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldloc, temp);
                _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                _il.Emit(OpCodes.Call, LocalSetMethods[(PrimitiveType)instr.ShortData]);

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
                _il.Emit(OpCodes.Call, LocalIncrement);
                // this is no-op for stack.
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void crossArray(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Array);
        switch (instr.Opcode)
        {
            case MTOpcode.iaload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(int)));
                }

                break;
            case MTOpcode.laload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(long)));
                }

                break;
            case MTOpcode.faload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(float)));
                }

                break;
            case MTOpcode.daload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(double)));
                }

                break;
            case MTOpcode.aaload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(Reference)));
                }

                break;
            case MTOpcode.baload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(sbyte)));
                    _il.Emit(OpCodes.Conv_I4);
                }

                break;
            case MTOpcode.caload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(char)));
                    _il.Emit(OpCodes.Conv_I4);
                }

                break;
            case MTOpcode.saload:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayGet.MakeGenericMethod(typeof(short)));
                    _il.Emit(OpCodes.Conv_I4);
                }

                break;
            case MTOpcode.iastore:
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(int)));
                break;
            case MTOpcode.lastore:
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(long)));
                break;
            case MTOpcode.fastore:
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(float)));
                break;
            case MTOpcode.dastore:
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(double)));
                break;
            case MTOpcode.aastore:
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(Reference)));
                break;
            case MTOpcode.bastore:
                _il.Emit(OpCodes.Conv_I1);
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(sbyte)));
                break;
            case MTOpcode.castore:
                _il.Emit(OpCodes.Conv_U2);
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(char)));
                break;
            case MTOpcode.sastore:
                _il.Emit(OpCodes.Conv_I2);
                _il.Emit(OpCodes.Call, ArraySet.MakeGenericMethod(typeof(short)));
                break;
            case MTOpcode.array_length:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Call, ArrayLength);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void crossStack(LinkedInstruction instr)
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
                var left = _il.DeclareLocal(StackTypes[LocalInstrIndex][^2].ToType());
                var right = _il.DeclareLocal(StackTypes[LocalInstrIndex][^1].ToType());

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
                var temp = _il.DeclareLocal(StackTypes[LocalInstrIndex][^1].ToType());
                _il.Emit(OpCodes.Stloc, temp);

                using (BeginMarshalSection(^2))
                {
                    _il.Emit(OpCodes.Ldloc, temp);
                }

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

    private void crossMath(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Math);
        switch (instr.Opcode)
        {
            case MTOpcode.iadd:
            case MTOpcode.ladd:
            case MTOpcode.fadd:
            case MTOpcode.dadd:
                _il.Emit(OpCodes.Add);
                EmitMathRepush();
                break;
            case MTOpcode.isub:
            case MTOpcode.lsub:
            case MTOpcode.fsub:
            case MTOpcode.dsub:
                _il.Emit(OpCodes.Sub);
                EmitMathRepush();
                break;
            case MTOpcode.imul:
            case MTOpcode.lmul:
            case MTOpcode.fmul:
            case MTOpcode.dmul:
                _il.Emit(OpCodes.Mul);
                EmitMathRepush();
                break;
            case MTOpcode.idiv:
            case MTOpcode.ldiv:
            case MTOpcode.fdiv:
            case MTOpcode.ddiv:
                _il.Emit(OpCodes.Div);
                EmitMathRepush();
                break;

            case MTOpcode.irem:
            case MTOpcode.lrem:
            case MTOpcode.frem:
            case MTOpcode.drem:
                _il.Emit(OpCodes.Rem);
                EmitMathRepush();
                break;

            case MTOpcode.ineg:
            case MTOpcode.lneg:
            case MTOpcode.fneg:
            case MTOpcode.dneg:
                _il.Emit(OpCodes.Neg);
                EmitMathRepush();
                break;

            case MTOpcode.ishl:
                _il.Emit(OpCodes.Ldc_I4, 31);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shl);
                EmitMathRepush();
                break;

            case MTOpcode.lshl:
                _il.Emit(OpCodes.Ldc_I4, 63);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shl);
                EmitMathRepush();
                break;

            case MTOpcode.ishr:
                _il.Emit(OpCodes.Ldc_I4, 31);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shr);
                EmitMathRepush();
                break;

            case MTOpcode.lshr:
                _il.Emit(OpCodes.Ldc_I4, 63);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shr);
                EmitMathRepush();
                break;

            case MTOpcode.iushr:
                _il.Emit(OpCodes.Ldc_I4, 31);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shr_Un);
                EmitMathRepush();
                break;

            case MTOpcode.lushr:
                _il.Emit(OpCodes.Ldc_I4, 63);
                _il.Emit(OpCodes.And);
                _il.Emit(OpCodes.Shr_Un);
                EmitMathRepush();
                break;

            case MTOpcode.iand:
            case MTOpcode.land:
                _il.Emit(OpCodes.And);
                EmitMathRepush();
                break;

            case MTOpcode.ior:
            case MTOpcode.lor:
                _il.Emit(OpCodes.Or);
                EmitMathRepush();
                break;

            case MTOpcode.ixor:
            case MTOpcode.lxor:
                _il.Emit(OpCodes.Xor);
                EmitMathRepush();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void crossConversion(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Conversion);
        switch (instr.Opcode)
        {
            case MTOpcode.i2l:
                _il.Emit(OpCodes.Conv_I8);
                EmitMathRepush();
                break;

            case MTOpcode.i2f:
                _il.Emit(OpCodes.Conv_R4);
                EmitMathRepush();
                break;

            case MTOpcode.i2d:
                _il.Emit(OpCodes.Conv_R8);
                EmitMathRepush();
                break;

            case MTOpcode.l2i:
                _il.Emit(OpCodes.Conv_I4);
                EmitMathRepush();
                break;

            case MTOpcode.l2f:
                _il.Emit(OpCodes.Conv_R4);
                EmitMathRepush();
                break;

            case MTOpcode.l2d:
                _il.Emit(OpCodes.Conv_R8);
                EmitMathRepush();
                break;

            case MTOpcode.f2i:
                _il.Emit(OpCodes.Call, typeof(JavaRunner).GetMethod(nameof(JavaRunner.F2I))!);
                EmitMathRepush();
                break;

            case MTOpcode.f2l:
                _il.Emit(OpCodes.Call, typeof(JavaRunner).GetMethod(nameof(JavaRunner.F2L))!);
                EmitMathRepush();
                break;

            case MTOpcode.f2d:
                _il.Emit(OpCodes.Conv_R8);
                EmitMathRepush();
                break;

            case MTOpcode.d2i:
                _il.Emit(OpCodes.Call, typeof(JavaRunner).GetMethod(nameof(JavaRunner.D2I))!);
                EmitMathRepush();
                break;

            case MTOpcode.d2l:
                _il.Emit(OpCodes.Call, typeof(JavaRunner).GetMethod(nameof(JavaRunner.D2L))!);
                EmitMathRepush();
                break;

            case MTOpcode.d2f:
                _il.Emit(OpCodes.Conv_R4);
                EmitMathRepush();
                break;

            case MTOpcode.i2b:
                _il.Emit(OpCodes.Conv_I1);
                _il.Emit(OpCodes.Conv_I4);
                EmitMathRepush();
                break;

            case MTOpcode.i2c:
                _il.Emit(OpCodes.Conv_U2);
                _il.Emit(OpCodes.Conv_I4);
                EmitMathRepush();
                break;

            case MTOpcode.i2s:
                _il.Emit(OpCodes.Conv_I2);
                _il.Emit(OpCodes.Conv_I4);
                EmitMathRepush();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void EmitMathRepush()
    {
        var purp = StackPurposes[LocalInstrIndex + 1][^1];
        switch (purp)
        {
            case StackValuePurpose.Consume:
            case StackValuePurpose.ToLocal:
                return;
        }

        _il.BeginScope();
        var temp = _il.DeclareLocal(StackTypes[LocalInstrIndex + 1][^1].ToType());
        _il.Emit(OpCodes.Stloc, temp);
        using (BeginMarshalSection(^1))
        {
            _il.Emit(OpCodes.Ldloc, temp);
        }

        _il.EndScope();
    }

    private void crossThrow(LinkedInstruction instr)
    {
        if (instr.Opcode != MTOpcode.athrow)
            throw new ArgumentOutOfRangeException();

        _il.Emit(OpCodes.Call, ThrowEx);
    }

    private void crossStatic(LinkedInstruction instr)
    {
        Debug.Assert(instr.Opcode.GetOpcodeType() == OpcodeType.Static);
        switch (instr.Opcode)
        {
            case MTOpcode.get_static:
                using (BeginMarshalSection(^1))
                {
                    _il.Emit(OpCodes.Ldsfld, Context);
                    _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                    _il.Emit(OpCodes.Call, StaticGetMethods[StackTypes[LocalInstrIndex + 1][^1]]);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
