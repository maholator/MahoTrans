// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using java.lang;
using MahoTrans.Runtime.Config;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Array = java.lang.Array;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

public class JavaRunner
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Step(JavaThread thread, JvmState state)
    {
        try
        {
            Thread.CurrentThread = thread;
            StepInternalV2(thread, state);
        }
        catch (JavaThrowable ex)
        {
            ProcessThrow(thread, state, ex);
        }
    }

    public static void ProcessThrow(JavaThread thread, JvmState state, JavaThrowable ex)
    {
        var throwFrame = thread.ActiveFrame!;
        var t = state.Resolve<Throwable>(ex.Throwable);

        if (HandleException(throwFrame, throwFrame.Pointer, t))
        {
            // handled in the same frame ( try { throw } catch { } )
            state.Toolkit.Logger?.LogExceptionCatch(ex.Throwable);
            return;
        }

        // unhandled in this frame: checking lower frames
        var frame = throwFrame;
        while (true)
        {
            thread.Pop(); // discarding frame
            if (thread.ActiveFrame == null)
            {
                // no more frames: aborting interpreter
                var exRealMsg = state.ResolveStringOrNull(t.Message);
                var exMsg = string.IsNullOrEmpty(exRealMsg)
                    ? "Exception has no attached message."
                    : $"Message: {exRealMsg}";
                throw new JavaUnhandledException($"Unhandled JVM exception \"{t.JavaClass}\": {exMsg}", t);
            }

            if (frame.Method.Method.IsCritical)
                ExitSynchronizedMethod(frame, thread.ActiveFrame, thread, state);
            frame = thread.ActiveFrame!;

            var lf = thread.ActiveFrame;
            if (HandleException(lf, lf.Pointer - 1, t))
            {
                // handled
                state.Toolkit.Logger?.LogExceptionCatch(ex.Throwable);
                return;
            }

            // next frame...
        }
    }

    private static bool HandleException(Frame frame, int pointer, Throwable t)
    {
        foreach (var linkedCatch in frame.Method.LinkedCatches)
        {
            if (!linkedCatch.IsIn(pointer))
                continue;

            if (t.JavaClass.Is(linkedCatch.ExceptionType))
            {
                frame.Pointer = linkedCatch.CatchStart;
                frame.DiscardAll();
                frame.PushReference(t.This);
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void StepInternalV2(JavaThread thread, JvmState jvm)
    {
        var frame = thread.ActiveFrame!;
        ref var pointer = ref frame.Pointer;
        var code = frame.Method.LinkedCode;

        Debug.Assert(pointer >= 0, $"Instruction pointer underflow in {frame.Method}");
        Debug.Assert(pointer < code.Length, $"Instruction pointer overflow in {frame.Method}");

        var instr = code[pointer];
        var opcode = instr.Opcode;
        switch (opcode)
        {
            case MTOpcode.nop:
                pointer++;
                break;

            case MTOpcode.iconst_m1:
                frame.PushInt(-1);
                pointer++;
                break;

            case MTOpcode.aconst_0:
            case MTOpcode.iconst_0:
                frame.PushInt(0);
                pointer++;
                break;

            case MTOpcode.iconst_1:
                frame.PushInt(1);
                pointer++;
                break;

            case MTOpcode.iconst_2:
                frame.PushInt(2);
                pointer++;
                break;

            case MTOpcode.iconst_3:
                frame.PushInt(3);
                pointer++;
                break;

            case MTOpcode.iconst_4:
                frame.PushInt(4);
                pointer++;
                break;

            case MTOpcode.iconst_5:
                frame.PushInt(5);
                pointer++;
                break;

            case MTOpcode.lconst_0:
                frame.PushLong(0);
                pointer++;
                break;

            case MTOpcode.lconst_1:
                frame.PushLong(1);
                pointer++;
                break;

            case MTOpcode.lconst_2:
                frame.PushLong(2);
                pointer++;
                break;

            case MTOpcode.fconst_0:
                frame.PushFloat(0);
                pointer++;
                break;

            case MTOpcode.fconst_1:
                frame.PushFloat(1);
                pointer++;
                break;

            case MTOpcode.fconst_2:
                frame.PushFloat(2);
                pointer++;
                break;

            case MTOpcode.dconst_0:
                frame.PushDouble(0);
                pointer++;
                break;

            case MTOpcode.dconst_1:
                frame.PushDouble(1);
                pointer++;
                break;

            case MTOpcode.dconst_2:
                frame.PushDouble(2);
                pointer++;
                break;

            case MTOpcode.iconst:
            case MTOpcode.fconst:
                frame.PushInt(instr.IntData);
                pointer++;
                break;

            case MTOpcode.strconst:
                frame.PushReference(jvm.InternalizeString((string)instr.Data));
                pointer++;
                break;

            case MTOpcode.lconst:
                frame.PushLong((long)instr.Data);
                pointer++;
                break;

            case MTOpcode.dconst:
                frame.PushDouble((double)instr.Data);
                pointer++;
                break;

            case MTOpcode.load:
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;

            case MTOpcode.store:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;

            case MTOpcode.iinc:
                frame.IncrementLocal(instr.ShortData, instr.IntData);
                pointer++;
                break;

            case MTOpcode.iaload:
                PushFromIntArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.laload:
                PushFromLongArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.faload:
                PushFromFloatArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.daload:
                PushFromDoubleArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.aaload:
                PushFromRefArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.baload:
                PushFromByteArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.caload:
                PushFromCharArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.saload:
                PushFromShortArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.iastore:
                PopToIntArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.lastore:
                PopToLongArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.fastore:
                PopToFloatArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.dastore:
                PopToDoubleArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.aastore:
                PopToRefArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.bastore:
                PopToByteArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.castore:
                PopToCharArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.sastore:
                PopToShortArray(frame, jvm);
                pointer++;
                break;

            case MTOpcode.array_length:
            {
                frame.PushInt(frame.PopReference().As<Array>().Length);
                pointer++;
                break;
            }

            case MTOpcode.pop:
                frame.StackTop--;
                pointer++;
                break;

            case MTOpcode.pop2:
                frame.StackTop -= 2;
                pointer++;
                break;

            case MTOpcode.swap:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v2);
                pointer++;
                break;
            }

            case MTOpcode.dup:
                unsafe
                {
                    frame.Stack[frame.StackTop] = frame.Stack[frame.StackTop - 1];
                    frame.StackTop++;
                    pointer++;
                    break;
                }

            case MTOpcode.dup2:
            {
                var v = frame.Pop();
                var v2 = frame.Pop();
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v);
                pointer++;
                break;
            }

            case MTOpcode.dup_x1:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v1);
                pointer++;
                break;
            }

            case MTOpcode.dup_x2:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                var v3 = frame.Pop();
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v3);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v1);
                pointer++;
                break;
            }

            case MTOpcode.dup2_x1:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                var v3 = frame.Pop();
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v3);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v1);
                pointer++;
                break;
            }

            case MTOpcode.iadd:
                frame.PushInt(frame.PopInt() + frame.PopInt());
                pointer++;
                break;

            case MTOpcode.ladd:
                frame.PushLong(frame.PopLong() + frame.PopLong());
                pointer++;
                break;

            case MTOpcode.fadd:
                frame.PushFloat(frame.PopFloat() + frame.PopFloat());
                pointer++;
                break;

            case MTOpcode.dadd:
                frame.PushDouble(frame.PopDouble() + frame.PopDouble());
                pointer++;
                break;

            case MTOpcode.isub:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 - val2);
                pointer++;
                break;
            }

            case MTOpcode.lsub:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 - val2);
                pointer++;
                break;
            }

            case MTOpcode.fsub:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 - val2);
                pointer++;
                break;
            }

            case MTOpcode.dsub:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 - val2);
                pointer++;
                break;
            }

            case MTOpcode.imul:
                frame.PushInt(frame.PopInt() * frame.PopInt());
                pointer++;
                break;

            case MTOpcode.lmul:
                frame.PushLong(frame.PopLong() * frame.PopLong());
                pointer++;
                break;

            case MTOpcode.fmul:
                frame.PushFloat(frame.PopFloat() * frame.PopFloat());
                pointer++;
                break;

            case MTOpcode.dmul:
                frame.PushDouble(frame.PopDouble() * frame.PopDouble());
                pointer++;
                break;

            case MTOpcode.idiv:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 / val2);
                pointer++;
                break;
            }

            case MTOpcode.ldiv:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 / val2);
                pointer++;
                break;
            }

            case MTOpcode.fdiv:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 / val2);
                pointer++;
                break;
            }

            case MTOpcode.ddiv:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 / val2);
                pointer++;
                break;
            }

            case MTOpcode.irem:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 % val2);
                pointer++;
                break;
            }

            case MTOpcode.lrem:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 % val2);
                pointer++;
                break;
            }

            case MTOpcode.frem:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 % val2);
                pointer++;
                break;
            }

            case MTOpcode.drem:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 % val2);
                pointer++;
                break;
            }

            case MTOpcode.ineg:
            {
                var v = frame.PopInt();
                frame.PushInt(-v);
                pointer++;
                break;
            }

            case MTOpcode.lneg:
            {
                var v = frame.PopLong();
                frame.PushLong(-v);
                pointer++;
                break;
            }

            case MTOpcode.fneg:
            {
                var v = frame.PopFloat();
                frame.PushFloat(-v);
                pointer++;
                break;
            }

            case MTOpcode.dneg:
            {
                var v = frame.PopDouble();
                frame.PushDouble(-v);
                pointer++;
                break;
            }

            case MTOpcode.ishl:
            {
                var val2 = frame.PopInt() & 0x1F;
                var val1 = frame.PopInt();
                frame.PushInt(val1 << val2);
                pointer++;
                break;
            }

            case MTOpcode.lshl:
            {
                var val2 = frame.PopInt() & 0x3F;
                var val1 = frame.PopLong();
                frame.PushLong(val1 << val2);
                pointer++;
                break;
            }

            case MTOpcode.ishr:
            {
                var val2 = frame.PopInt() & 0x1F;
                var val1 = frame.PopInt();
                frame.PushInt(val1 >> val2);
                pointer++;
                break;
            }

            case MTOpcode.lshr:
            {
                var val2 = frame.PopInt() & 0x3F;
                var val1 = frame.PopLong();
                frame.PushLong(val1 >> val2);
                pointer++;
                break;
            }

            case MTOpcode.iushr:
            {
                var val2 = frame.PopInt() & 0x1F;
                var val1 = (uint)frame.PopInt();
                var r = val1 >> val2;
                frame.PushInt((int)r);
                pointer++;
                break;
            }

            case MTOpcode.lushr:
            {
                var val2 = frame.PopInt() & 0x3F;
                var val1 = (ulong)frame.PopLong();
                var r = val1 >> val2;
                frame.PushLong((long)r);
                pointer++;
                break;
            }

            case MTOpcode.iand:
                frame.PushInt(frame.PopInt() & frame.PopInt());
                pointer++;
                break;

            case MTOpcode.land:
                frame.PushLong(frame.PopLong() & frame.PopLong());
                pointer++;
                break;

            case MTOpcode.ior:
                frame.PushInt(frame.PopInt() | frame.PopInt());
                pointer++;
                break;

            case MTOpcode.lor:
                frame.PushLong(frame.PopLong() | frame.PopLong());
                pointer++;
                break;

            case MTOpcode.ixor:
                frame.PushInt(frame.PopInt() ^ frame.PopInt());
                pointer++;
                break;

            case MTOpcode.lxor:
                frame.PushLong(frame.PopLong() ^ frame.PopLong());
                pointer++;
                break;

            case MTOpcode.i2l:
                frame.PushLong(frame.PopInt());
                pointer++;
                break;

            case MTOpcode.i2f:
                frame.PushFloat(frame.PopInt());
                pointer++;
                break;

            case MTOpcode.i2d:
                frame.PushDouble(frame.PopInt());
                pointer++;
                break;

            case MTOpcode.l2i:
            {
                ulong ul = ((ulong)frame.PopLong()) & 0xFF_FF_FF_FF;
                frame.PushInt((int)(uint)ul);
                pointer++;
                break;
            }

            case MTOpcode.l2f:
                frame.PushFloat(frame.PopLong());
                pointer++;
                break;

            case MTOpcode.l2d:
                frame.PushDouble(frame.PopLong());
                pointer++;
                break;

            case MTOpcode.f2i:
                frame.PushInt(F2I(frame.PopFloat()));
                pointer++;
                break;

            case MTOpcode.f2l:
                frame.PushLong(F2L(frame.PopFloat()));
                pointer++;
                break;

            case MTOpcode.f2d:
                frame.PushDouble(frame.PopFloat());
                pointer++;
                break;

            case MTOpcode.d2i:
                DoubleToInt(frame);
                pointer++;
                break;

            case MTOpcode.d2l:
                DoubleToLong(frame);
                pointer++;
                break;

            case MTOpcode.d2f:
                frame.PushFloat((float)frame.PopDouble());
                pointer++;
                break;

            case MTOpcode.i2b:
            {
                var val = frame.PopInt() & 0xFF;
                frame.PushInt((sbyte)(byte)val);
                pointer++;
                break;
            }

            case MTOpcode.i2c:
                frame.PushInt((char)frame.PopInt());
                pointer++;
                break;

            case MTOpcode.i2s:
            {
                var b = (int)(short)(ushort)(uint)frame.PopInt();
                frame.PushInt(b);
                pointer++;
                break;
            }

            case MTOpcode.lcmp:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }

            case MTOpcode.fcmpl:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                if (float.IsNaN(val1) || float.IsNaN(val2))
                    frame.PushInt(-1);
                else
                    frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }

            case MTOpcode.fcmpg:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                if (float.IsNaN(val1) || float.IsNaN(val2))
                    frame.PushInt(1);
                else
                    frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }

            case MTOpcode.dcmpl:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                if (double.IsNaN(val1) || double.IsNaN(val2))
                    frame.PushInt(-1);
                else
                    frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }

            case MTOpcode.dcmpg:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                if (double.IsNaN(val1) || double.IsNaN(val2))
                    frame.PushInt(1);
                else
                    frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }

            case MTOpcode.ifeq:
                pointer = frame.PopInt() == 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.ifne:
                pointer = frame.PopInt() != 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.iflt:
                pointer = frame.PopInt() < 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.ifge:
                pointer = frame.PopInt() >= 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.ifgt:
                pointer = frame.PopInt() > 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.ifle:
                pointer = frame.PopInt() <= 0 ? instr.IntData : pointer + 1;
                break;

            case MTOpcode.if_cmpeq:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 == val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.if_cmpne:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 != val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.if_cmplt:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 < val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.if_cmpge:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 >= val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.if_cmpgt:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 > val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.if_cmple:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 <= val2 ? instr.IntData : pointer + 1;
                break;
            }

            case MTOpcode.tableswitch:
            {
                var ia = (int[])instr.Data;
                int low = ia[1];
                int high = ia[2];

                var value = frame.PopInt();
                if (value < low || value > high)
                {
                    pointer = ia[0];
                }
                else
                {
                    int branchNum = value - low;
                    int offset = ia[branchNum + 3];
                    pointer = offset;
                }

                break;
            }

            case MTOpcode.lookupswitch:
            {
                var ia = (int[])instr.Data;
                int count = ia[1];

                var i = 2;
                var value = frame.PopInt();
                for (int j = 0; j < count; j++)
                {
                    if (ia[i] == value)
                    {
                        // this branch
                        pointer = ia[i + 1];
                        goto lookupExit;
                    }

                    // not this branch
                    i += 2;
                }

                pointer = ia[0];

                lookupExit: ;
                break;
            }

            case MTOpcode.jump:
                pointer = instr.IntData;
                break;

            case MTOpcode.return_value:
            {
                var returnValue = frame.Pop();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, jvm);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushUnchecked(returnValue);
                }

                break;
            }

            case MTOpcode.return_void:
            {
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, jvm);

                if (caller != null)
                {
                    caller.Pointer++;
                }

                break;
            }

            case MTOpcode.return_void_inplace:
            {
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, jvm);

                break;
            }

            case MTOpcode.athrow:
                jvm.Throw(frame.PopReference());
                break;

            case MTOpcode.invoke_virtual:
                CallVirtual(instr.IntData, instr.ShortData, frame, thread, jvm);
                break;

            case MTOpcode.invoke_static:
                CallMethod((Method)instr.Data, true, frame, thread);
                break;

            case MTOpcode.invoke_instance:
                CallMethod((Method)instr.Data, false, frame, thread);
                break;

            case MTOpcode.invoke_virtual_void_no_args_bysig:
                CallVirtBySig(thread, jvm, frame);
                break;

            case MTOpcode.new_obj:
            {
                var type = (JavaClass)instr.Data;
                frame.PushReference(jvm.AllocateObject(type));
                pointer++;
                break;
            }

            case MTOpcode.new_prim_arr:
            {
                int len = frame.PopInt();
                frame.PushReference(jvm.AllocateArray((ArrayType)instr.IntData, len));
                pointer++;
                break;
            }

            case MTOpcode.new_arr:
            {
                int len = frame.PopInt();
                var type = (JavaClass)instr.Data;
                frame.PushReference(jvm.AllocateReferenceArray(len, type));
                pointer++;
                break;
            }

            case MTOpcode.new_multi_arr:
            {
                var cls = (JavaClass)instr.Data;
                var dims = instr.IntData;
                int[] count = new int[dims];
                for (int i = 0; i < count.Length; i++)
                {
                    count[i] = frame.PopInt();
                }

                var underlyingType = cls.Name.Substring(dims);
                ArrayType? arrayType = underlyingType switch
                {
                    "I" => ArrayType.T_INT,
                    "J" => ArrayType.T_LONG,
                    "C" => ArrayType.T_CHAR,
                    "S" => ArrayType.T_SHORT,
                    "Z" => ArrayType.T_BOOLEAN,
                    "B" => ArrayType.T_BYTE,
                    "F" => ArrayType.T_FLOAT,
                    "D" => ArrayType.T_DOUBLE,
                    _ => null
                };

                frame.PushReference(CreateMultiSubArray(dims - 1, count, jvm, arrayType, cls));

                pointer++;
                break;
            }

            case MTOpcode.monitor_enter:
                TryEnterMonitor(thread, jvm, frame);
                break;

            case MTOpcode.monitor_exit:
            {
                var r = frame.PopReference();
                if (r.IsNull)
                    jvm.Throw<NullPointerException>();

                var obj = jvm.ResolveObject(r);
                obj.ExitMonitor(thread);
                pointer++;

                break;
            }

            case MTOpcode.checkcast:
                unsafe
                {
                    var type = (JavaClass)instr.Data;
                    var obj = (Reference)frame.Stack[frame.StackTop - 1];
                    if (obj.IsNull)
                    {
                        // ok
                    }
                    else if (jvm.ResolveObject(obj).JavaClass.Is(type))
                    {
                        // ok
                    }
                    else
                    {
                        jvm.Throw<ClassCastException>("Attempted cast of " + jvm.ResolveObject(obj).JavaClass + " to " +
                                                      type);
                    }

                    pointer++;
                    break;
                }

            case MTOpcode.instanceof:
            {
                var type = (JavaClass)instr.Data;

                var obj = frame.PopReference();
                if (obj.IsNull)
                    frame.PushInt(0);
                else
                    frame.PushInt(jvm.ResolveObject(obj).JavaClass.Is(type) ? 1 : 0);
                pointer++;
                break;
            }

            case MTOpcode.bridge:
            {
                ((Action<Frame>)instr.Data)(frame);
                pointer++;
                break;
            }
            case MTOpcode.bridge_init:
            {
                var p = (ClassBoundBridge)instr.Data;
                if (p.Class.PendingInitializer)
                {
                    p.Class.Initialize(thread);
                    return;
                }

                p.Bridge(frame);
                pointer++;
                break;
            }

            case MTOpcode.get_static_init:
            {
                var p = (JavaClass)instr.Data;
                if (p.PendingInitializer)
                {
                    p.Initialize(thread);
                    return;
                }

                frame.PushUnchecked(jvm.StaticFields[instr.IntData]);
                pointer++;
                break;
            }

            case MTOpcode.set_static_init:
            {
                var p = (JavaClass)instr.Data;
                if (p.PendingInitializer)
                {
                    p.Initialize(thread);
                    return;
                }

                jvm.StaticFields[instr.IntData] = frame.Pop();
                pointer++;
                break;
            }

            case MTOpcode.get_static:
                frame.PushUnchecked(jvm.StaticFields[instr.IntData]);
                pointer++;
                break;

            case MTOpcode.set_static:
                jvm.StaticFields[instr.IntData] = frame.Pop();
                pointer++;
                break;

            case MTOpcode.error_no_class:
            {
                if (jvm.MissingHandling == MissingThingsHandling.Crash)
                    throw new JavaRuntimeError($"Class {instr.Data} is not loaded!");

                jvm.Throw<NoClassDefFoundError>($"Class {instr.Data} is not loaded!");
                break;
            }

            case MTOpcode.error_no_method:
            {
                if (jvm.MissingHandling == MissingThingsHandling.Crash)
                    throw new JavaRuntimeError($"Method {instr.Data} is not found!");

                jvm.Throw<NoSuchMethodError>($"Method {instr.Data} is not found!");
                break;
            }

            case MTOpcode.error_no_field:
            {
                if (jvm.MissingHandling == MissingThingsHandling.Crash)
                    throw new JavaRuntimeError($"Field {instr.Data} is not found!");

                jvm.Throw<NoSuchFieldError>($"Field {instr.Data} is not found!");
                break;
            }

            case MTOpcode.error_bytecode:
                if (instr.Data == null!)
                    throw new JavaRuntimeError("Execution abort opcode was reached.");
                throw new JavaRuntimeError(instr.Data.ToString());
        }
    }

    /// <summary>
    ///     Exits monitor that was entered using <see cref="TryEnterInstanceMonitor" />.
    /// </summary>
    /// <param name="frame">Frame that was popped. This method will perform exit on this thread.</param>
    /// <param name="caller">Frame that called the method.</param>
    /// <param name="thread">Owner of the monitor.</param>
    /// <param name="state">JVM.</param>
    private static unsafe void ExitSynchronizedMethod(Frame frame, Frame? caller, JavaThread thread, JvmState state)
    {
        if (caller == null)
            return;
        Reference monitorHost;

        if (frame.Method.Method.IsStatic)
            monitorHost = frame.Method.Method.Class.GetOrInitModel();
        else
            monitorHost = caller.Stack[caller.StackTop];

        var obj = state.ResolveObject(monitorHost);
        obj.ExitMonitor(thread);
    }

    private static Reference CreateMultiSubArray(int dimensionsLeft, int[] count, JvmState state, ArrayType? typeP,
        JavaClass typeA)
    {
        if (dimensionsLeft == 0)
        {
            if (typeP.HasValue)
                return state.AllocateArray(typeP.Value, count[0]);

            return state.AllocateReferenceArray(count[0], typeA);
        }

        var r = state.AllocateReferenceArray(count[dimensionsLeft], typeA);

        var arr = state.ResolveArray<Reference>(r);
        var subType = state.GetClass(typeA.Name.Substring(1));
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = CreateMultiSubArray(dimensionsLeft - 1, count, state, typeP, subType);
        }

        return r;
    }

    /// <summary>
    ///     Attempts to enter object's monitor. Enters if possible. If not, this is no-op. Frame won't go to next instruction
    ///     if entrance failed.
    /// </summary>
    /// <param name="thread">Thread to enter.</param>
    /// <param name="state">JVM.</param>
    /// <param name="frame">Current frame.</param>
    /// <remarks>This api is for <see cref="JavaOpcode.monitorenter" /> opcode.</remarks>
    private static void TryEnterMonitor(JavaThread thread, JvmState state, Frame frame)
    {
        var r = frame.PopReference();
        if (r.IsNull)
            state.Throw<NullPointerException>();

        if (TryEnterInstanceMonitor(r, thread, state))
        {
            // if monitor entered, go to next opcode
            frame.Pointer++;
        }
        else
        {
            // else wait
            frame.PushReference(r);
            // not going to next instruction!
        }
    }

    /// <summary>
    ///     Attempts to enter object's monitor.
    /// </summary>
    /// <param name="r">Object to enter.</param>
    /// <param name="thread">Current thread.</param>
    /// <param name="state">JVM.</param>
    /// <returns>Returns true on success. Monitor must be exited then.</returns>
    /// <remarks>
    ///     This is for synchronized methods. When false returned, nothing must be done. One more attempt must be
    ///     attempted.
    /// </remarks>
    private static bool TryEnterInstanceMonitor(Reference r, JavaThread thread, JvmState state)
    {
        var obj = state.ResolveObject(r);
        return obj.TryEnterMonitor(thread);
    }

    #region Numbers manipulation

    public static int F2I(float f)
    {
        if (float.IsNaN(f))
            return 0;
        if (float.IsPositiveInfinity(f))
            return int.MaxValue;
        if (float.IsNegativeInfinity(f))
            return int.MinValue;
        if (float.IsFinite(f))
            return (int)f;

        throw new JavaRuntimeError($"Can't round float number {f}");
    }

    public static long F2L(float f)
    {
        if (float.IsNaN(f))
            return 0L;
        if (float.IsPositiveInfinity(f))
            return long.MaxValue;
        if (float.IsNegativeInfinity(f))
            return long.MinValue;
        if (float.IsFinite(f))
            return (long)f;

        throw new JavaRuntimeError($"Can't round float number {f}");
    }

    private static void DoubleToInt(Frame frame)
    {
        double val = frame.PopDouble();
        if (double.IsNaN(val))
            frame.PushInt(0);
        else if (double.IsFinite(val))
            frame.PushInt((int)val);
        else if (double.IsPositiveInfinity(val))
            frame.PushInt(int.MaxValue);
        else if (double.IsNegativeInfinity(val))
            frame.PushInt(int.MinValue);
        else
            throw new JavaRuntimeError($"Can't round double number {val}");
    }

    private static void DoubleToLong(Frame frame)
    {
        double val = frame.PopDouble();
        if (double.IsNaN(val))
            frame.PushLong(0);
        else if (double.IsFinite(val))
            frame.PushLong((long)val);
        else if (double.IsPositiveInfinity(val))
            frame.PushLong(long.MaxValue);
        else if (double.IsNegativeInfinity(val))
            frame.PushLong(long.MinValue);
        else
            throw new JavaRuntimeError($"Can't round double number {val}");
    }

    #endregion

    #region Array manipulation

    private static void PopToDoubleArray(Frame frame, JvmState state)
    {
        var value = frame.PopDouble();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToFloatArray(Frame frame, JvmState state)
    {
        var value = frame.PopFloat();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToRefArray(Frame frame, JvmState state)
    {
        var value = frame.PopReference();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToLongArray(Frame frame, JvmState state)
    {
        var value = frame.PopLong();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToIntArray(Frame frame, JvmState state)
    {
        var value = frame.PopInt();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToCharArray(Frame frame, JvmState state)
    {
        var value = frame.PopChar();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToShortArray(Frame frame, JvmState state)
    {
        var value = frame.PopShort();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.SetArrayElement(reference, index, value);
    }

    private static void PopToByteArray(Frame frame, JvmState state)
    {
        var value = frame.PopByte();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array>(reference);
        if (array is Array<bool> b)
            b[index] = value != 0;
        else if (array is Array<sbyte> s)
            s[index] = value;
        else
            throw new JavaRuntimeError();
    }

    private static void PushFromCharArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<char>>(reference);
        frame.PushChar(array[index]);
    }

    private static void PushFromShortArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<short>>(reference);
        frame.PushShort(array[index]);
    }

    private static void PushFromRefArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<Reference>>(reference);
        frame.PushReference(array[index]);
    }

    private static void PushFromDoubleArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<double>>(reference);
        frame.PushDouble(array[index]);
    }

    private static void PushFromFloatArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<float>>(reference);
        frame.PushFloat(array[index]);
    }

    private static void PushFromLongArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<long>>(reference);
        frame.PushLong(array[index]);
    }

    private static void PushFromIntArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<int>>(reference);
        frame.PushInt(array[index]);
    }

    private static void PushFromByteArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array>(reference);
        if (index < 0 || index >= array.BaseArray.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        if (array is Array<bool> b)
            frame.PushBool(b[index]);
        else if (array is Array<sbyte> s)
            frame.PushByte(s[index]);
        else
            throw new JavaRuntimeError($"Unknown byte array type: {array.GetType()}");
    }

    #endregion

    private static void CallVirtBySig(JavaThread thread, JvmState state, Frame frame)
    {
        // taking name & descriptor
        frame.SetFrom(2);
        var name = state.ResolveString(frame.PopReferenceFrom());
        var descr = state.ResolveString(frame.PopReferenceFrom());

        // resolving pointer
        var virtPoint = state.GetVirtualPointer(new NameDescriptor(name, descr));

        // target (1), name (1), decr (1)
        frame.SetFrom(3);

        // resolving object to check <clinit>
        var obj = state.ResolveObject(frame.PopReferenceFrom());
        var callClass = obj.JavaClass.VirtualTable![virtPoint]!.Class;
        if (callClass.PendingInitializer)
        {
            callClass.Initialize(thread);
            // we want to do this instruction again so no pointer increase here
            return;
        }

        // name & descriptor
        frame.Discard(2);

        // call
        CallVirtual(virtPoint, 0, frame, thread, state);
    }

    private static unsafe void CallVirtual(int pointer, ushort argsCount, Frame frame, JavaThread thread,
        JvmState state)
    {
        var i = frame.StackTop - (argsCount + 1);
        var obj = state.ResolveObject(frame.Stack[i]);

        var virtTable = obj.JavaClass.VirtualTable!;
        if (pointer >= virtTable.Length)
            ThrowUnresolvedVirtual(pointer, state, obj);
        var m = virtTable[pointer];
        if (m == null)
            ThrowUnresolvedVirtual(pointer, state, obj);

        CallMethod(m, false, frame, thread);
    }

    [DoesNotReturn]
    private static void ThrowUnresolvedVirtual(int pointer, JvmState state, Object obj)
    {
        throw new JavaRuntimeError(
            $"No virtual method {state.DecodeVirtualPointer(pointer)} found on object {obj.JavaClass.Name}");
    }

    private static void CallMethod(Method m, bool @static, Frame frame, JavaThread thread)
    {
        if (m.Class.PendingInitializer)
        {
            m.Class.Initialize(thread);
            // we want to do this instruction again so no pointer increase here
            return;
        }

        if (m.Bridge != null)
        {
            m.Bridge(frame);

            // we are done with the call, so going to next instruction
            frame.Pointer++;
            return;
        }

        var argsLength = m.ArgsCount;

        if (@static)
        {
            if (m.IsCritical)
            {
                var host = m.Class.GetOrInitModel();
                if (!TryEnterInstanceMonitor(host, thread, Object.Jvm))
                    return;
            }
        }
        else
        {
            argsLength += 1;

            if (m.IsCritical)
            {
                frame.SetFrom(argsLength);
                var r = frame.PopReferenceFrom();
                if (!TryEnterInstanceMonitor(r, thread, Object.Jvm))
                    return;
            }
        }

        var java = m.JavaBody;

        var f = thread.Push(java!);
        frame.SetFrom(argsLength);
        unsafe
        {
            var sizes = java!.ArgsSizes;
            var argIndex = 0;
            var localIndex = 0;
            for (; argIndex < argsLength; argIndex++)
            {
                f.LocalVariables[localIndex] = frame.PopUnknownFrom();
                localIndex += sizes[argIndex];
            }
        }

        frame.Discard(argsLength);
    }
}