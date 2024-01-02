using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using java.lang;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

public class JavaRunner
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Step(JavaThread thread, JvmState state)
    {
        //var frame = thread.ActiveFrame;
        // try
        // {
        try
        {
            Thread.CurrentThread = thread;
            StepInternal(thread, state);
        }
        catch (JavaThrowable ex)
        {
            ProcessThrow(thread, state, ex);
        }
        // }
        // catch
        // {
        //     Console.WriteLine($"Faulty instruction: {frame.Method.Code[frame.Pointer]}");
        //     Console.WriteLine("Call stack:");
        //     foreach (var frame1 in thread.CallStack)
        //     {
        //         Console.WriteLine(frame1);
        //     }
        //
        //     throw;
        // }
    }

    public static void ProcessThrow(JavaThread thread, JvmState state, JavaThrowable ex)
    {
        var throwFrame = thread.ActiveFrame!;
        var t = state.Resolve<Throwable>(ex.Throwable);
        state.Toolkit.Logger.LogDebug(DebugMessageCategory.Exceptions, $"Exception {t.JavaClass.Name} is caught");
        Console.WriteLine("Call stack:");
        for (int i = thread.ActiveFrameIndex; i >= 0; i--)
        {
            Console.WriteLine(thread.CallStack[i]);
        }

        if (HandleException(throwFrame, throwFrame.Pointer, t))
        {
            // handled
            return;
        }

        var frame = throwFrame;
        // unhandled
        while (true)
        {
            thread.Pop(); // discarding frame
            if (thread.ActiveFrame == null)
            {
                Console.WriteLine(ex);
                // no more frames

                var exRealMsg = state.ResolveStringOrDefault(t.Message);
                var exMsg = string.IsNullOrEmpty(exRealMsg)
                    ? "Exception has no attached message."
                    : $"Message: {exRealMsg}";
                var exSource =
                    $"{throwFrame.Method}:{throwFrame.Pointer} ({throwFrame.Method.Code[throwFrame.Pointer]})";
                var message = $"Unhandled JVM exception \"{t.JavaClass}\" at {exSource}\n{exMsg}";
                throw new JavaUnhandledException(message, ex);
            }

            if (frame.Method.Method.IsCritical)
                ExitSynchronizedMethod(frame, thread.ActiveFrame, thread, state);
            frame = thread.ActiveFrame!;

            var lf = thread.ActiveFrame;
            if (HandleException(lf, lf.Pointer - 1, t))
            {
                // handled
                return;
            }

            // next frame...
        }
    }

    private static bool HandleException(Frame frame, int pointer, Throwable t)
    {
        var instr = frame.Method.Code[pointer];

        foreach (var @catch in frame.Method.Catches)
        {
            if (@catch.IsIn(instr))
            {
                string allowedType = (string)frame.Method.Method.Class.Constants[@catch.Type];
                if (t.JavaClass.Is(allowedType))
                {
                    var code = frame.Method.Code;
                    var tByte = @catch.CatchStart;
                    if (tByte < pointer)
                    {
                        for (var j = pointer; j >= 0; j--)
                        {
                            if (code[j].Offset == tByte)
                            {
                                frame.Pointer = j;
                                goto push;
                            }
                        }
                    }
                    else
                    {
                        for (var j = pointer; j < code.Length; j++)
                        {
                            if (code[j].Offset == tByte)
                            {
                                frame.Pointer = j;
                                goto push;
                            }
                        }
                    }
                }
            }
        }

        return false;

        // pushing exception back to stack
        push:
        frame.DiscardAll();
        frame.PushReference(t.This);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void StepInternal(JavaThread thread, JvmState state)
    {
        var frame = thread.ActiveFrame!;
        ref var pointer = ref frame.Pointer;
        var code = frame.Method.LinkedCode;

        Debug.Assert(pointer >= 0, $"Instruction pointer underflow in {frame.Method}");
        Debug.Assert(pointer < code.Length, $"Instruction pointer overflow in {frame.Method}");

        var instr = code[pointer];
        switch (instr.Opcode)
        {
            case JavaOpcode.nop:
                pointer++;
                break;
            case JavaOpcode.aconst_null:
                frame.PushReference(Reference.Null);
                pointer++;
                break;
            case JavaOpcode.iconst_m1:
                frame.PushInt(-1);
                pointer++;
                break;
            case JavaOpcode.iconst_0:
                frame.PushInt(0);
                pointer++;
                break;
            case JavaOpcode.iconst_1:
                frame.PushInt(1);
                pointer++;
                break;
            case JavaOpcode.iconst_2:
                frame.PushInt(2);
                pointer++;
                break;
            case JavaOpcode.iconst_3:
                frame.PushInt(3);
                pointer++;
                break;
            case JavaOpcode.iconst_4:
                frame.PushInt(4);
                pointer++;
                break;
            case JavaOpcode.iconst_5:
                frame.PushInt(5);
                pointer++;
                break;
            case JavaOpcode.lconst_0:
                frame.PushLong(0L);
                pointer++;
                break;
            case JavaOpcode.lconst_1:
                frame.PushLong(1L);
                pointer++;
                break;
            case JavaOpcode.fconst_0:
                frame.PushFloat(0f);
                pointer++;
                break;
            case JavaOpcode.fconst_1:
                frame.PushFloat(1f);
                pointer++;
                break;
            case JavaOpcode.fconst_2:
                frame.PushFloat(2f);
                pointer++;
                break;
            case JavaOpcode.dconst_0:
                frame.PushDouble(0d);
                pointer++;
                break;
            case JavaOpcode.dconst_1:
                frame.PushDouble(1d);
                pointer++;
                break;
            case JavaOpcode.bipush:
            case JavaOpcode.sipush:
            {
                frame.PushInt(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.ldc:
            case JavaOpcode.ldc_w:
            case JavaOpcode.ldc2_w:
            {
                state.PushClassConstant(frame, instr.Data);
                pointer++;
                break;
            }
            case JavaOpcode.iload:
            {
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.lload:
            {
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.fload:
            {
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.dload:
            {
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.aload:
            {
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;
            }
            case JavaOpcode.iload_0:
                frame.PushFromLocal(0);
                pointer++;
                break;
            case JavaOpcode.iload_1:
                frame.PushFromLocal(1);
                pointer++;
                break;
            case JavaOpcode.iload_2:
                frame.PushFromLocal(2);
                pointer++;
                break;
            case JavaOpcode.iload_3:
                frame.PushFromLocal(3);
                pointer++;
                break;
            case JavaOpcode.lload_0:
                frame.PushFromLocal(0);
                pointer++;
                break;
            case JavaOpcode.lload_1:
                frame.PushFromLocal(1);
                pointer++;
                break;
            case JavaOpcode.lload_2:
                frame.PushFromLocal(2);
                pointer++;
                break;
            case JavaOpcode.lload_3:
                frame.PushFromLocal(3);
                pointer++;
                break;
            case JavaOpcode.fload_0:
                frame.PushFromLocal(0);
                pointer++;
                break;
            case JavaOpcode.fload_1:
                frame.PushFromLocal(1);
                pointer++;
                break;
            case JavaOpcode.fload_2:
                frame.PushFromLocal(2);
                pointer++;
                break;
            case JavaOpcode.fload_3:
                frame.PushFromLocal(3);
                pointer++;
                break;
            case JavaOpcode.dload_0:
                frame.PushFromLocal(0);
                pointer++;
                break;
            case JavaOpcode.dload_1:
                frame.PushFromLocal(1);
                pointer++;
                break;
            case JavaOpcode.dload_2:
                frame.PushFromLocal(2);
                pointer++;
                break;
            case JavaOpcode.dload_3:
                frame.PushFromLocal(3);
                pointer++;
                break;
            case JavaOpcode.aload_0:
                frame.PushFromLocal(0);
                pointer++;
                break;
            case JavaOpcode.aload_1:
                frame.PushFromLocal(1);
                pointer++;
                break;
            case JavaOpcode.aload_2:
                frame.PushFromLocal(2);
                pointer++;
                break;
            case JavaOpcode.aload_3:
                frame.PushFromLocal(3);
                pointer++;
                break;
            case JavaOpcode.iaload:
                PushFromIntArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.laload:
                PushFromLongArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.faload:
                PushFromFloatArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.daload:
                PushFromDoubleArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.aaload:
                PushFromRefArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.baload:
                PushFromByteArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.caload:
                PushFromCharArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.saload:
                PushFromShortArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.istore:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;
            case JavaOpcode.lstore:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;
            case JavaOpcode.fstore:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;
            case JavaOpcode.dstore:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;
            case JavaOpcode.astore:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;
            case JavaOpcode.istore_0:
            {
                frame.PopToLocal(0);
                pointer++;
                break;
            }
            case JavaOpcode.istore_1:
            {
                frame.PopToLocal(1);
                pointer++;
                break;
            }
            case JavaOpcode.istore_2:
            {
                frame.PopToLocal(2);
                pointer++;
                break;
            }
            case JavaOpcode.istore_3:
            {
                frame.PopToLocal(3);
                pointer++;
                break;
            }
            case JavaOpcode.lstore_0:
            {
                frame.PopToLocal(0);
                pointer++;
                break;
            }
            case JavaOpcode.lstore_1:
                frame.PopToLocal(1);
                pointer++;
                break;
            case JavaOpcode.lstore_2:
                frame.PopToLocal(2);
                pointer++;
                break;
            case JavaOpcode.lstore_3:
                frame.PopToLocal(3);
                pointer++;
                break;
            case JavaOpcode.fstore_0:
                frame.PopToLocal(0);
                pointer++;
                break;
            case JavaOpcode.fstore_1:
                frame.PopToLocal(1);
                pointer++;
                break;
            case JavaOpcode.fstore_2:
                frame.PopToLocal(2);
                pointer++;
                break;
            case JavaOpcode.fstore_3:
                frame.PopToLocal(3);
                pointer++;
                break;
            case JavaOpcode.dstore_0:
                frame.PopToLocal(0);
                pointer++;
                break;
            case JavaOpcode.dstore_1:
                frame.PopToLocal(1);
                pointer++;
                break;
            case JavaOpcode.dstore_2:
                frame.PopToLocal(2);
                pointer++;
                break;
            case JavaOpcode.dstore_3:
                frame.PopToLocal(3);
                pointer++;
                break;
            case JavaOpcode.astore_0:
                frame.PopToLocal(0);
                pointer++;
                break;
            case JavaOpcode.astore_1:
                frame.PopToLocal(1);
                pointer++;
                break;
            case JavaOpcode.astore_2:
                frame.PopToLocal(2);
                pointer++;
                break;
            case JavaOpcode.astore_3:
                frame.PopToLocal(3);
                pointer++;
                break;
            case JavaOpcode.iastore:
                PopToIntArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.lastore:
                PopToLongArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.fastore:
                PopToFloatArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.dastore:
                PopToDoubleArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.aastore:
                PopToRefArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.bastore:
                PopToByteArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.castore:
                PopToCharArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.sastore:
                PopToShortArray(frame, state);
                pointer++;
                break;
            case JavaOpcode.pop:
            {
                frame.StackTop--;
                pointer++;
                break;
            }
            case JavaOpcode.pop2:
            {
                frame.StackTop--;
                if (instr.IntData != 0)
                    frame.StackTop--;

                pointer++;
                break;
            }
            case JavaOpcode.dup:
                unsafe
                {
                    frame.Stack[frame.StackTop] = frame.Stack[frame.StackTop - 1];
                    frame.StackTop++;
                    pointer++;
                    break;
                }
            case JavaOpcode.dup_x1:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v1);
                pointer++;
                break;
            }
            case JavaOpcode.dup_x2:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                if (instr.IntData != 0)
                {
                    frame.PushUnchecked(v1);
                    frame.PushUnchecked(v2);
                    frame.PushUnchecked(v1);
                }
                else
                {
                    var v3 = frame.Pop();
                    frame.PushUnchecked(v1);
                    frame.PushUnchecked(v3);
                    frame.PushUnchecked(v2);
                    frame.PushUnchecked(v1);
                }

                pointer++;
                break;
            }
            case JavaOpcode.dup2:
            {
                var v = frame.Pop();
                if (instr.IntData != 0)
                {
                    frame.PushUnchecked(v);
                    frame.PushUnchecked(v);
                    pointer++;
                    break;
                }

                var v2 = frame.Pop();
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v);
                frame.PushUnchecked(v2);
                frame.PushUnchecked(v);
                pointer++;
                break;
            }
            case JavaOpcode.dup2_x1:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                if (instr.IntData != 0)
                {
                    frame.PushUnchecked(v1);
                    frame.PushUnchecked(v2);
                    frame.PushUnchecked(v1);
                }
                else
                {
                    var v3 = frame.Pop();
                    frame.PushUnchecked(v2);
                    frame.PushUnchecked(v1);
                    frame.PushUnchecked(v3);
                    frame.PushUnchecked(v2);
                    frame.PushUnchecked(v1);
                }

                pointer++;
                break;
            }
            case JavaOpcode.dup2_x2:
                throw new NotImplementedException("No dup2_x2 opcode");
            case JavaOpcode.swap:
            {
                var v1 = frame.Pop();
                var v2 = frame.Pop();
                frame.PushUnchecked(v1);
                frame.PushUnchecked(v2);
                pointer++;
                break;
            }
            case JavaOpcode.iadd:
                frame.PushInt(frame.PopInt() + frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.ladd:
                frame.PushLong(frame.PopLong() + frame.PopLong());
                pointer++;
                break;
            case JavaOpcode.fadd:
                frame.PushFloat(frame.PopFloat() + frame.PopFloat());
                pointer++;
                break;
            case JavaOpcode.dadd:
                frame.PushDouble(frame.PopDouble() + frame.PopDouble());
                pointer++;
                break;
            case JavaOpcode.isub:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 - val2);
                pointer++;
                break;
            }
            case JavaOpcode.lsub:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 - val2);
                pointer++;
                break;
            }
            case JavaOpcode.fsub:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 - val2);
                pointer++;
                break;
            }
            case JavaOpcode.dsub:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 - val2);
                pointer++;
                break;
            }
            case JavaOpcode.imul:
                frame.PushInt(frame.PopInt() * frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.lmul:
                frame.PushLong(frame.PopLong() * frame.PopLong());
                pointer++;
                break;
            case JavaOpcode.fmul:
                frame.PushFloat(frame.PopFloat() * frame.PopFloat());
                pointer++;
                break;
            case JavaOpcode.dmul:
                frame.PushDouble(frame.PopDouble() * frame.PopDouble());
                pointer++;
                break;
            case JavaOpcode.idiv:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 / val2);
                pointer++;
                break;
            }
            case JavaOpcode.ldiv:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 / val2);
                pointer++;
                break;
            }
            case JavaOpcode.fdiv:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 / val2);
                pointer++;
                break;
            }
            case JavaOpcode.ddiv:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 / val2);
                pointer++;
                break;
            }
            case JavaOpcode.irem:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 % val2);
                pointer++;
                break;
            }
            case JavaOpcode.lrem:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 % val2);
                pointer++;
                break;
            }
            case JavaOpcode.frem:
            {
                var val2 = frame.PopFloat();
                var val1 = frame.PopFloat();
                frame.PushFloat(val1 % val2);
                pointer++;
                break;
            }
            case JavaOpcode.drem:
            {
                var val2 = frame.PopDouble();
                var val1 = frame.PopDouble();
                frame.PushDouble(val1 % val2);
                pointer++;
                break;
            }
            case JavaOpcode.ineg:
            {
                var v = frame.PopInt();
                frame.PushInt(-v);
                pointer++;
                break;
            }
            case JavaOpcode.lneg:
            {
                var v = frame.PopLong();
                frame.PushLong(-v);
                pointer++;
                break;
            }
            case JavaOpcode.fneg:
            {
                var v = frame.PopFloat();
                frame.PushFloat(-v);
                pointer++;
                break;
            }
            case JavaOpcode.dneg:
            {
                var v = frame.PopDouble();
                frame.PushDouble(-v);
                pointer++;
                break;
            }
            case JavaOpcode.ishl:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 << val2);
                pointer++;
                break;
            }
            case JavaOpcode.lshl:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                frame.PushLong(val1 << val2);
                pointer++;
                break;
            }
            case JavaOpcode.ishr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 >> val2);
                pointer++;
                break;
            }
            case JavaOpcode.lshr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                frame.PushLong(val1 >> val2);
                pointer++;
                break;
            }
            case JavaOpcode.iushr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                var r = (uint)val1 >> val2;
                frame.PushInt((int)r);
                pointer++;
                break;
            }
            case JavaOpcode.lushr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                var r = (ulong)val1 >> val2;
                frame.PushLong((long)r);
                pointer++;
                break;
            }
            case JavaOpcode.iand:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 & val2);
                pointer++;
                break;
            }
            case JavaOpcode.land:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 & val2);
                pointer++;
                break;
            }
            case JavaOpcode.ior:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 | val2);
                pointer++;
                break;
            }
            case JavaOpcode.lor:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 | val2);
                pointer++;
                break;
            }
            case JavaOpcode.ixor:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 ^ val2);
                pointer++;
                break;
            }
            case JavaOpcode.lxor:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushLong(val1 ^ val2);
                pointer++;
                break;
            }
            case JavaOpcode.iinc:
                unsafe
                {
                    long val = frame.LocalVariables[instr.ShortData];
                    var i = (int)val;
                    i += (sbyte)instr.IntData;
                    frame.LocalVariables[instr.ShortData] = i;
                    pointer++;
                    break;
                }
            case JavaOpcode.i2l:
                frame.PushLong(frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.i2f:
                frame.PushFloat(frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.i2d:
                frame.PushDouble(frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.l2i:
            {
                ulong ul = ((ulong)frame.PopLong()) & 0xFF_FF_FF_FF;
                frame.PushInt((int)(uint)ul);
                pointer++;
                break;
            }
            case JavaOpcode.l2f:
                frame.PushFloat(frame.PopLong());
                pointer++;
                break;
            case JavaOpcode.l2d:
                frame.PushDouble(frame.PopLong());
                pointer++;
                break;
            case JavaOpcode.f2i:
                FloatToInt(frame);
                pointer++;
                break;
            case JavaOpcode.f2l:
                FloatToLong(frame);
                pointer++;
                break;
            case JavaOpcode.f2d:
                frame.PushDouble(frame.PopFloat());
                pointer++;
                break;
            case JavaOpcode.d2i:
                DoubleToInt(frame);
                pointer++;
                break;
            case JavaOpcode.d2l:
                DoubleToLong(frame);
                pointer++;
                break;
            case JavaOpcode.d2f:
                frame.PushFloat((float)frame.PopDouble());
                pointer++;
                break;
            case JavaOpcode.i2b:
            {
                var val = frame.PopInt() & 0xFF;
                frame.PushInt((sbyte)(byte)val);
                pointer++;
                break;
            }
            case JavaOpcode.i2c:
                frame.PushInt((char)frame.PopInt());
                pointer++;
                break;
            case JavaOpcode.i2s:
            {
                var b = (int)(short)(ushort)(uint)frame.PopInt();
                frame.PushInt(b);
                pointer++;
                break;
            }
            case JavaOpcode.lcmp:
            {
                var val2 = frame.PopLong();
                var val1 = frame.PopLong();
                frame.PushInt(val1.CompareTo(val2));
                pointer++;
                break;
            }
            case JavaOpcode.fcmpl:
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
            case JavaOpcode.fcmpg:
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
            case JavaOpcode.dcmpl:
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
            case JavaOpcode.dcmpg:
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
            case JavaOpcode.ifeq:
            {
                var val = frame.PopInt();
                pointer = val == 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.ifne:
            {
                var val = frame.PopInt();
                pointer = val != 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.iflt:
            {
                var val = frame.PopInt();
                pointer = val < 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.ifge:
            {
                var val = frame.PopInt();
                pointer = val >= 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.ifgt:
            {
                var val = frame.PopInt();
                pointer = val > 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.ifle:
            {
                var val = frame.PopInt();
                pointer = val <= 0 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmpeq:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 == val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmpne:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 != val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmplt:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 < val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmpge:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 >= val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmpgt:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 > val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_icmple:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                pointer = val1 <= val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_acmpeq:
            {
                var val1 = frame.PopReference();
                var val2 = frame.PopReference();
                pointer = val1 == val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.if_acmpne:
            {
                var val1 = frame.PopReference();
                var val2 = frame.PopReference();
                pointer = val1 != val2 ? instr.IntData : pointer + 1;
                break;
            }
            case JavaOpcode.@goto:
                pointer = instr.IntData;
                break;
            case JavaOpcode.jsr:
                throw new NotImplementedException("No jsr opcode");
            case JavaOpcode.ret:
                throw new NotImplementedException("No ret opcode");
            case JavaOpcode.tableswitch:
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
            case JavaOpcode.lookupswitch:
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
            case JavaOpcode.ireturn:
            {
                var returnee = frame.PopInt();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushInt(returnee);
                }

                break;
            }
            case JavaOpcode.lreturn:
            {
                var returnee = frame.PopLong();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushLong(returnee);
                }

                break;
            }
            case JavaOpcode.freturn:
            {
                var returnee = frame.PopFloat();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushFloat(returnee);
                }

                break;
            }
            case JavaOpcode.dreturn:
            {
                var returnee = frame.PopDouble();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushDouble(returnee);
                }


                break;
            }
            case JavaOpcode.areturn:
            {
                var returnee = frame.PopReference();
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                    caller.PushReference(returnee);
                }

                break;
            }
            case JavaOpcode.@return:
            {
                thread.Pop();
                var caller = thread.ActiveFrame;

                if (frame.Method.Method.IsCritical)
                    ExitSynchronizedMethod(frame, caller, thread, state);

                if (caller != null)
                {
                    caller.Pointer++;
                }

                break;
            }
            case JavaOpcode.getstatic:
            case JavaOpcode.putstatic:
            case JavaOpcode.getfield:
            case JavaOpcode.putfield:
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
            case JavaOpcode.invokevirtual:
                CallVirtual(instr.IntData, instr.ShortData, frame, thread, state);
                break;
            case JavaOpcode.invokespecial:
                CallMethod((Method)instr.Data, false, frame, thread);
                break;
            case JavaOpcode.invokestatic:
                CallMethod((Method)instr.Data, true, frame, thread);
                break;
            case JavaOpcode.invokeinterface:
                CallVirtual(instr.IntData, instr.ShortData, frame, thread, state);
                break;
            case JavaOpcode.invokedynamic:
                throw new JavaRuntimeError("Dynamic invoke is not supported");
            case JavaOpcode.newobject:
            {
                var type = (JavaClass)instr.Data;
                var r = state.AllocateObject(type);
                frame.PushReference(r);
                pointer++;
                break;
            }
            case JavaOpcode.newarray:
            {
                int len = frame.PopInt();
                frame.PushReference(state.AllocateArray((ArrayType)instr.IntData, len));
                pointer++;
                break;
            }
            case JavaOpcode.anewarray:
            {
                int len = frame.PopInt();
                var type = (JavaClass)instr.Data;
                frame.PushReference(state.AllocateReferenceArray(len, type));
                pointer++;
                break;
            }
            case JavaOpcode.arraylength:
            {
                var arr = state.Resolve<java.lang.Array>(frame.PopReference());
                frame.PushInt(arr.BaseValue.Length);
                pointer++;
                break;
            }
            case JavaOpcode.athrow:
            {
                var ex = frame.PopReference();
                if (ex.IsNull)
                    state.Throw<NullPointerException>();
                else
                {
                    state.Toolkit.Logger.LogDebug(DebugMessageCategory.Exceptions, $"athrow opcode executed");
                    throw new JavaThrowable(ex);
                }

                break;
            }
            case JavaOpcode.checkcast:
                unsafe
                {
                    var type = (JavaClass)instr.Data;
                    var obj = (Reference)frame.Stack[frame.StackTop - 1];
                    if (obj.IsNull)
                    {
                        // ok
                    }
                    else if (state.ResolveObject(obj).JavaClass.Is(type))
                    {
                        // ok
                    }
                    else
                    {
                        state.Throw<ClassCastException>();
                    }

                    pointer++;
                    break;
                }
            case JavaOpcode.instanceof:
            {
                var type = (JavaClass)instr.Data;

                var obj = frame.PopReference();
                if (obj.IsNull)
                    frame.PushInt(0);
                else
                    frame.PushInt(state.ResolveObject(obj).JavaClass.Is(type) ? 1 : 0);
                pointer++;
                break;
            }
            case JavaOpcode.monitorenter:
            {
                TryEnterMonitor(thread, state, frame);
                break;
            }
            case JavaOpcode.monitorexit:
            {
                var r = frame.PopReference();
                if (r.IsNull)
                    state.Throw<NullPointerException>();

                var obj = state.ResolveObject(r);
                if (obj.MonitorOwner != thread.ThreadId)
                {
                    state.Throw<IllegalMonitorStateException>();
                }
                else
                {
                    obj.MonitorReEnterCount--;
                    if (obj.MonitorReEnterCount == 0)
                        obj.MonitorOwner = 0;
                }

                pointer++;

                break;
            }
            case JavaOpcode.wide:
                var aargs = (byte[])instr.Data;
                var op = (JavaOpcode)aargs[0];
                if (op == JavaOpcode.iinc)
                {
                    unsafe
                    {
                        var index = BytecodeLinker.Combine(aargs[1], aargs[2]);
                        var i = (int)frame.LocalVariables[index];
                        i += BytecodeLinker.Combine(aargs[3], aargs[4]);
                        frame.LocalVariables[index] = i;
                    }
                }
                else
                {
                    var i = BytecodeLinker.Combine(aargs[1], aargs[2]);
                    switch (op)
                    {
                        case JavaOpcode.aload:
                            frame.PushFromLocal(i);
                            break;
                        case JavaOpcode.iload:
                            frame.PushFromLocal(i);
                            break;
                        case JavaOpcode.lload:
                            frame.PushFromLocal(i);
                            break;
                        case JavaOpcode.fload:
                            frame.PushFromLocal(i);
                            break;
                        case JavaOpcode.dload:
                            frame.PushFromLocal(i);
                            break;
                        case JavaOpcode.astore:
                            frame.PopToLocal(i);
                            break;
                        case JavaOpcode.istore:
                            frame.PopToLocal(i);
                            break;
                        case JavaOpcode.lstore:
                            frame.PopToLocal(i);
                            break;
                        case JavaOpcode.fstore:
                            frame.PopToLocal(i);
                            break;
                        case JavaOpcode.dstore:
                            frame.PopToLocal(i);
                            break;
                        default:
                            throw new JavaRuntimeError($"Invalid wide opcode {op}");
                    }
                }

                pointer++;
                break;
            case JavaOpcode.multianewarray:
            {
                var d = (MultiArrayInitializer)instr.Data;
                var dims = d.dimensions;
                int[] count = new int[dims];
                for (int i = 0; i < count.Length; i++)
                {
                    count[i] = frame.PopInt();
                }

                var underlyingType = d.type.Name.Substring(dims);
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

                frame.PushReference(CreateMultiSubArray(dims - 1, count, state, arrayType, d.type));

                pointer++;
                break;
            }
            case JavaOpcode.ifnull:
                pointer = frame.PopReference().IsNull ? instr.IntData : pointer + 1;
                break;
            case JavaOpcode.ifnonnull:
                pointer = frame.PopReference().IsNull == false ? instr.IntData : pointer + 1;
                break;
            case JavaOpcode.goto_w:
                throw new NotImplementedException("No goto opcode");
            case JavaOpcode.jsr_w:
                throw new NotImplementedException("No jsr opcode");
            case JavaOpcode.breakpoint:
                throw new NotImplementedException("No breakpoint opcode");
            case JavaOpcode._inplacereturn:
                thread.Pop();
                break;
            case JavaOpcode._invokeany:
            {
                CallVirtBySig(thread, state, frame);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void StepInternalV2(JavaThread thread, JvmState state)
    {
        var frame = thread.ActiveFrame!;
        ref var pointer = ref frame.Pointer;
        var code = frame.Method.LinkedCode;

        Debug.Assert(pointer >= 0, $"Instruction pointer underflow in {frame.Method}");
        Debug.Assert(pointer < code.Length, $"Instruction pointer overflow in {frame.Method}");

        var instr = code[pointer];
        var opcode = (MTOpcode)instr.Opcode; //TODO
        switch (opcode)
        {
            case MTOpcode.nop:
                pointer++;
                break;

            case MTOpcode.iconst_m1:
                frame.PushInt(-1);
                pointer++;
                break;

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
                frame.PushInt(instr.IntData);
                pointer++;
                break;

            case MTOpcode.mconst:
                state.PushClassConstant(frame, instr.Data);
                pointer++;
                break;

            case MTOpcode.load:
                frame.PushFromLocal(instr.IntData);
                pointer++;
                break;

            case MTOpcode.load_0:
                frame.PushFromLocal(0);
                pointer++;
                break;

            case MTOpcode.load_1:
                frame.PushFromLocal(1);
                pointer++;
                break;

            case MTOpcode.load_2:
                frame.PushFromLocal(2);
                pointer++;
                break;

            case MTOpcode.load_3:
                frame.PushFromLocal(3);
                pointer++;
                break;

            case MTOpcode.store:
                frame.PopToLocal(instr.IntData);
                pointer++;
                break;

            case MTOpcode.store_0:
                frame.PopToLocal(0);
                pointer++;
                break;

            case MTOpcode.store_1:
                frame.PopToLocal(1);
                pointer++;
                break;

            case MTOpcode.store_2:
                frame.PopToLocal(2);
                pointer++;
                break;

            case MTOpcode.store_3:
                frame.PopToLocal(3);
                pointer++;
                break;

            case MTOpcode.iinc:
                unsafe
                {
                    long val = frame.LocalVariables[instr.ShortData];
                    var i = (int)val;
                    i += (sbyte)instr.IntData;
                    frame.LocalVariables[instr.ShortData] = i;
                    pointer++;
                    break;
                }

            case MTOpcode.iaload:
                PushFromIntArray(frame, state);
                pointer++;
                break;

            case MTOpcode.laload:
                PushFromLongArray(frame, state);
                pointer++;
                break;

            case MTOpcode.faload:
                PushFromFloatArray(frame, state);
                pointer++;
                break;

            case MTOpcode.daload:
                PushFromDoubleArray(frame, state);
                pointer++;
                break;

            case MTOpcode.aaload:
                PushFromRefArray(frame, state);
                pointer++;
                break;

            case MTOpcode.baload:
                PushFromByteArray(frame, state);
                pointer++;
                break;

            case MTOpcode.caload:
                PushFromCharArray(frame, state);
                pointer++;
                break;

            case MTOpcode.saload:
                PushFromShortArray(frame, state);
                pointer++;
                break;

            case MTOpcode.iastore:
                PopToIntArray(frame, state);
                pointer++;
                break;

            case MTOpcode.lastore:
                PopToLongArray(frame, state);
                pointer++;
                break;

            case MTOpcode.fastore:
                PopToFloatArray(frame, state);
                pointer++;
                break;

            case MTOpcode.dastore:
                PopToDoubleArray(frame, state);
                pointer++;
                break;

            case MTOpcode.aastore:
                PopToRefArray(frame, state);
                pointer++;
                break;

            case MTOpcode.bastore:
                PopToByteArray(frame, state);
                pointer++;
                break;

            case MTOpcode.castore:
                PopToCharArray(frame, state);
                pointer++;
                break;

            case MTOpcode.sastore:
                PopToShortArray(frame, state);
                pointer++;
                break;

            case MTOpcode.array_length:
            {
                var arr = state.Resolve<java.lang.Array>(frame.PopReference());
                frame.PushInt(arr.BaseValue.Length);
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
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 << val2);
                pointer++;
                break;
            }

            case MTOpcode.lshl:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                frame.PushLong(val1 << val2);
                pointer++;
                break;
            }

            case MTOpcode.ishr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                frame.PushInt(val1 >> val2);
                pointer++;
                break;
            }

            case MTOpcode.lshr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                frame.PushLong(val1 >> val2);
                pointer++;
                break;
            }

            case MTOpcode.iushr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopInt();
                var r = (uint)val1 >> val2;
                frame.PushInt((int)r);
                pointer++;
                break;
            }

            case MTOpcode.lushr:
            {
                var val2 = frame.PopInt();
                var val1 = frame.PopLong();
                var r = (ulong)val1 >> val2;
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
                FloatToInt(frame);
                pointer++;
                break;

            case MTOpcode.f2l:
                FloatToLong(frame);
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
                    ExitSynchronizedMethod(frame, caller, thread, state);

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
                    ExitSynchronizedMethod(frame, caller, thread, state);

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
                    ExitSynchronizedMethod(frame, caller, thread, state);

                break;
            }

            case MTOpcode.athrow:
            {
                var ex = frame.PopReference();
                if (ex.IsNull)
                    state.Throw<NullPointerException>();
                else
                {
                    state.Toolkit.Logger.LogDebug(DebugMessageCategory.Exceptions, "athrow opcode executed");
                    throw new JavaThrowable(ex);
                }

                break;
            }

            case MTOpcode.invoke_virt:
                CallVirtual(instr.IntData, instr.ShortData, frame, thread, state);
                break;

            case MTOpcode.invoke_static:
                CallMethod((Method)instr.Data, true, frame, thread);
                break;

            case MTOpcode.invoke_instance:
                CallMethod((Method)instr.Data, false, frame, thread);
                break;

            case MTOpcode.invoke_instance_void_no_args_bysig:
                CallVirtBySig(thread, state, frame);
                break;

            case MTOpcode.new_obj:
            {
                var type = (JavaClass)instr.Data;
                frame.PushReference(state.AllocateObject(type));
                pointer++;
                break;
            }

            case MTOpcode.new_prim_arr:
            {
                int len = frame.PopInt();
                frame.PushReference(state.AllocateArray((ArrayType)instr.IntData, len));
                pointer++;
                break;
            }

            case MTOpcode.new_arr:
            {
                int len = frame.PopInt();
                var type = (JavaClass)instr.Data;
                frame.PushReference(state.AllocateReferenceArray(len, type));
                pointer++;
                break;
            }

            case MTOpcode.new_multi_arr:
            {
                var d = (MultiArrayInitializer)instr.Data;
                var dims = d.dimensions;
                int[] count = new int[dims];
                for (int i = 0; i < count.Length; i++)
                {
                    count[i] = frame.PopInt();
                }

                var underlyingType = d.type.Name.Substring(dims);
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

                frame.PushReference(CreateMultiSubArray(dims - 1, count, state, arrayType, d.type));

                pointer++;
                break;
            }

            case MTOpcode.monitor_enter:
                TryEnterMonitor(thread, state, frame);
                break;

            case MTOpcode.monitor_exit:
            {
                var r = frame.PopReference();
                if (r.IsNull)
                    state.Throw<NullPointerException>();

                var obj = state.ResolveObject(r);
                if (obj.MonitorOwner != thread.ThreadId)
                {
                    state.Throw<IllegalMonitorStateException>();
                }
                else
                {
                    obj.MonitorReEnterCount--;
                    if (obj.MonitorReEnterCount == 0)
                        obj.MonitorOwner = 0;
                }

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
                    else if (state.ResolveObject(obj).JavaClass.Is(type))
                    {
                        // ok
                    }
                    else
                    {
                        state.Throw<ClassCastException>();
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
                    frame.PushInt(state.ResolveObject(obj).JavaClass.Is(type) ? 1 : 0);
                pointer++;
                break;
            }

            case MTOpcode.bridge:
            {
                ((Action<Frame>)instr.Data)(frame);
                pointer++;
                break;
            }
            case MTOpcode.bridge_init_class:
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
        }
    }

    /// <summary>
    /// Exits monitor that was entered using <see cref="TryEnterInstanceMonitor"/>.
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
        if (obj.MonitorOwner != thread.ThreadId)
        {
            state.Throw<IllegalMonitorStateException>();
        }
        else
        {
            obj.MonitorReEnterCount--;
            if (obj.MonitorReEnterCount == 0)
                obj.MonitorOwner = 0;
        }
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
    /// Attempts to enter object's monitor. Enters if possible. If not, this is no-op. Frame won't go to next instruction if entrance failed.
    /// </summary>
    /// <param name="thread">Thread to enter.</param>
    /// <param name="state">JVM.</param>
    /// <param name="frame">Current frame.</param>
    /// <remarks>This api is for <see cref="JavaOpcode.monitorenter"/> opcode.</remarks>
    private static void TryEnterMonitor(JavaThread thread, JvmState state, Frame frame)
    {
        var r = frame.PopReference();
        if (r.IsNull)
            state.Throw<NullPointerException>();

        var obj = state.ResolveObject(r);
        if (obj.MonitorReEnterCount == 0)
        {
            obj.MonitorOwner = thread.ThreadId;
            obj.MonitorReEnterCount = 1;
            frame.Pointer++;
        }
        else if (obj.MonitorOwner == thread.ThreadId)
        {
            obj.MonitorReEnterCount++;
            frame.Pointer++;
        }
        else
        {
            // wait
            frame.PushReference(r);
            // not going to next instruction!
        }
    }

    /// <summary>
    /// Attempts to enter object's monitor.
    /// </summary>
    /// <param name="r">Object to enter.</param>
    /// <param name="thread">Current thread.</param>
    /// <param name="state">JVM.</param>
    /// <returns>Returns true on success. Monitor must be exited then.</returns>
    /// <remarks>This is for synchronized methods. When false returned, nothing must be done. One more attempt must be attempted.</remarks>
    private static bool TryEnterInstanceMonitor(Reference r, JavaThread thread, JvmState state)
    {
        var obj = state.ResolveObject(r);
        if (obj.MonitorReEnterCount == 0)
        {
            obj.MonitorOwner = thread.ThreadId;
            obj.MonitorReEnterCount = 1;
            return true;
        }

        if (obj.MonitorOwner == thread.ThreadId)
        {
            obj.MonitorReEnterCount++;
            return true;
        }

        return false;
    }

    #region Numbers manipulation

    private static void FloatToInt(Frame frame)
    {
        float val = frame.PopFloat();
        if (float.IsNaN(val))
            frame.PushInt(0);
        else if (float.IsFinite(val))
            frame.PushInt((int)val);
        else if (float.IsPositiveInfinity(val))
            frame.PushInt(int.MaxValue);
        else if (float.IsNegativeInfinity(val))
            frame.PushInt(int.MinValue);
        else
            throw new JavaRuntimeError($"Can't round float number {val}");
    }

    private static void FloatToLong(Frame frame)
    {
        float val = frame.PopFloat();
        if (float.IsNaN(val))
            frame.PushLong(0);
        else if (float.IsFinite(val))
            frame.PushLong((long)val);
        else if (float.IsPositiveInfinity(val))
            frame.PushLong(long.MaxValue);
        else if (float.IsNegativeInfinity(val))
            frame.PushLong(long.MinValue);
        else
            throw new JavaRuntimeError($"Can't round float number {val}");
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
        state.ResolveArray<double>(reference)[index] = value;
    }

    private static void PopToFloatArray(Frame frame, JvmState state)
    {
        var value = frame.PopFloat();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<float>(reference)[index] = value;
    }

    private static void PopToRefArray(Frame frame, JvmState state)
    {
        var value = frame.PopReference();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<Reference>(reference)[index] = value;
    }

    private static void PopToLongArray(Frame frame, JvmState state)
    {
        var value = frame.PopLong();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<long>(reference)[index] = value;
    }

    private static void PopToIntArray(Frame frame, JvmState state)
    {
        var value = frame.PopInt();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<int>(reference)[index] = value;
    }

    private static void PopToCharArray(Frame frame, JvmState state)
    {
        var value = frame.PopChar();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<char>(reference)[index] = value;
    }

    private static void PopToShortArray(Frame frame, JvmState state)
    {
        var value = frame.PopShort();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        state.ResolveArray<short>(reference)[index] = value;
    }

    private static void PopToByteArray(Frame frame, JvmState state)
    {
        var value = frame.PopByte();
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<java.lang.Array>(reference);
        if (array is Array<bool> b)
            b.Value[index] = value != 0;
        else if (array is Array<sbyte> s)
            s.Value[index] = value;
        else
            throw new JavaRuntimeError();
    }

    private static void PushFromCharArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<char>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushChar(array[index]);
    }

    private static void PushFromShortArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<short>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushShort(array[index]);
    }

    private static void PushFromRefArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<Reference>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushReference(array[index]);
    }

    private static void PushFromDoubleArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<double>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushDouble(array[index]);
    }

    private static void PushFromFloatArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<float>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushFloat(array[index]);
    }

    private static void PushFromLongArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<long>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushLong(array[index]);
    }

    private static void PushFromIntArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<Array<int>>(reference).Value;
        if (index < 0 || index >= array.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        frame.PushInt(array[index]);
    }

    private static void PushFromByteArray(Frame frame, JvmState state)
    {
        var index = frame.PopInt();
        var reference = frame.PopReference();
        var array = state.Resolve<java.lang.Array>(reference);
        if (index < 0 || index >= array.BaseValue.Length)
            state.Throw<ArrayIndexOutOfBoundsException>();
        if (array is Array<bool> b)
            frame.PushBool(b.Value[index]);
        else if (array is Array<sbyte> s)
            frame.PushByte(s.Value[index]);
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

        m.JavaBody.EnsureBytecodeLinked();
        var f = thread.Push(m.JavaBody);
        frame.SetFrom(argsLength);
        for (var arg = 0; arg < argsLength; arg++)
        {
            unsafe
            {
                f.LocalVariables[arg] = frame.PopUnknownFrom();
            }
        }

        frame.Discard(argsLength);
    }
}