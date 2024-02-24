// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MahoTrans.Runtime.Errors;

namespace MahoTrans.Runtime;

/// <summary>
///     JVM call stack frame. Keeps reference to running method, execution pointer, stack and locals.
/// </summary>
public unsafe class Frame
{
    /// <summary>
    ///     Method which is run in this frame.
    /// </summary>
    public JavaMethodBody Method;

    /// <summary>
    ///     Pointer to next instruction to execute. To get instruction for execution, do <see cref="Method" />.
    ///     <see cref="JavaMethodBody.LinkedCode" />[<see cref="Pointer" />]. If the frame is executed right now, this points
    ///     to running opcode, it will be increased only after execution.
    /// </summary>
    public int Pointer;

    /// <summary>
    ///     Buffer with local variables. Be careful to not corrupt memory around it.
    /// </summary>
    public long* LocalVariables;

    /// <summary>
    ///     Buffer with stack. Be careful to not corrupt memory around it.
    /// </summary>
    public long* Stack = null;

    /// <summary>
    ///     Pointer to stack top. Stack contains topmost operand at this index-1 (if zero, stack is empty). Stack length is
    ///     equal to this field.
    /// </summary>
    public int StackTop;

    /// <summary>
    ///     Length of <see cref="Stack" />. Never touch it manually!
    /// </summary>
    public ushort CurrentStackSize;

    /// <summary>
    ///     Length of <see cref="LocalVariables" />. Never touch it manually!
    /// </summary>
    public ushort CurrentLocalsSize;

    public const ushort INIT_BUF_SIZE = 16;

    public Frame(JavaMethodBody method)
    {
        Method = method;
        // we are trying to alloc at least INIT_BUF_SIZE size buffer, but our method may request more.
        AllocateLocals(Math.Max(method.LocalsCount, INIT_BUF_SIZE));
        AllocateStack(Math.Max(method.StackSize, INIT_BUF_SIZE));
        ClearBuffers(method.LocalsCount, method.StackSize);
    }

    #region Native buffers management

    public void Reinitialize(JavaMethodBody method)
    {
        StackTop = 0;
        Pointer = 0;

        if (Method == method)
        {
            ClearBuffers(method.LocalsCount, method.StackSize);
            return;
        }

        Method = method;
        if (CurrentLocalsSize < method.LocalsCount)
        {
            DeallocateLocals();
            AllocateLocals(method.LocalsCount);
        }

        if (CurrentStackSize < method.StackSize)
        {
            DeallocateStack();
            AllocateStack(method.StackSize);
        }

        ClearBuffers(method.LocalsCount, method.StackSize);
    }

    /// <summary>
    ///     Fills native buffers with zeros.
    /// </summary>
    /// <param name="locals">Locals count.</param>
    /// <param name="stack">Stack size.</param>
    private void ClearBuffers(ushort locals, ushort stack)
    {
        Unsafe.InitBlock(LocalVariables, 0, (uint)(locals * sizeof(long)));
        Unsafe.InitBlock(Stack, 0, (uint)(stack * sizeof(long)));
    }

    /// <summary>
    ///     Allocates locals buffer.
    /// </summary>
    /// <param name="locals">Count of locals.</param>
    private void AllocateLocals(ushort locals)
    {
        LocalVariables = (long*)NativeMemory.Alloc(locals, sizeof(long));
        CurrentLocalsSize = locals;
    }

    /// <summary>
    ///     Allocates stack buffer.
    /// </summary>
    /// <param name="stack">Stack size.</param>
    private void AllocateStack(ushort stack)
    {
        Stack = (long*)NativeMemory.Alloc(stack, sizeof(long));
        CurrentStackSize = stack;
    }


    /// <summary>
    ///     Deallocates locals buffer. This will be automatically done on object destruction.
    /// </summary>
    private void DeallocateLocals()
    {
        if (LocalVariables != null)
            NativeMemory.Free(LocalVariables);
        LocalVariables = null;
    }

    /// <summary>
    ///     Deallocates stack buffer. This will be automatically done on object destruction.
    /// </summary>
    private void DeallocateStack()
    {
        if (Stack != null)
            NativeMemory.Free(Stack);
        Stack = null;
    }

    ~Frame()
    {
        DeallocateLocals();
        DeallocateStack();
    }

    #endregion

    #region Dump API

    public long[] DumpStack()
    {
        var s = new long[Method.StackSize];
        fixed (long* sPtr = s)
        {
            Buffer.MemoryCopy(Stack, sPtr, s.Length * sizeof(long), s.Length * sizeof(long));
        }

        return s;
    }

    public long[] DumpLocalVariables()
    {
        var s = new long[Method.LocalsCount];
        fixed (long* sPtr = s)
        {
            Buffer.MemoryCopy(LocalVariables, sPtr, s.Length * sizeof(long), s.Length * sizeof(long));
        }

        return s;
    }

    #endregion

    public void DiscardAll() => StackTop = 0;

    #region Basic stack operations

    /// <summary>
    ///     Pushes value into the stack.
    /// </summary>
    /// <param name="value">Value to push.</param>
    public void PushUnchecked(long value)
    {
#if DEBUG
        if (StackTop >= Method.StackSize)
            throw new JavaRuntimeError($"Stack overflow in {Method.Method}");
#endif
        Stack[StackTop] = value;
        StackTop++;
    }

    public long Pop()
    {
#if DEBUG
        if (StackTop == 0)
            throw new JavaRuntimeError($"Stack underflow in {Method.Method}");
#endif
        StackTop--;
        return Stack[StackTop];
    }

    #endregion

    #region Universal methods

    /// <summary>
    ///     Pushes unknown value into the stack.
    /// </summary>
    /// <param name="value">Value to push.</param>
    /// <exception cref="JavaRuntimeError">Value has unsupported type.</exception>
    public void Push(object? value)
    {
        if (value is null)
        {
            PushUnchecked(0);
            return;
        }

        if (value is int i)
        {
            PushInt(i);
            return;
        }

        if (value is long l)
        {
            PushLong(l);
            return;
        }

        if (value is float f)
        {
            PushFloat(f);
            return;
        }

        if (value is double d)
        {
            PushDouble(d);
            return;
        }

        if (value is Reference r)
        {
            PushReference(r);
            return;
        }

        if (value is bool b)
        {
            PushBool(b);
            return;
        }

        if (value is char c)
        {
            PushInt(c);
            return;
        }

        if (value is short s)
        {
            PushInt(s);
            return;
        }

        if (value is sbyte sb)
        {
            PushInt(sb);
            return;
        }

        throw new JavaRuntimeError($"Type {value.GetType()} can't be pushed on stack.");
    }

    #endregion

    #region Type-specific pushes

    public void PushInt(int value) => PushUnchecked(value);

    public void PushLong(long value) => PushUnchecked(value);

    public void PushFloat(float value) => PushUnchecked(BitConverter.SingleToInt32Bits(value));

    public void PushDouble(double value) => PushUnchecked(BitConverter.DoubleToInt64Bits(value));

    public void PushBool(bool value) => PushUnchecked(value ? 1L : 0L);

    public void PushByte(sbyte value) => PushUnchecked(value);

    public void PushShort(short value) => PushUnchecked(value);

    public void PushChar(char value) => PushUnchecked(value);

    public void PushReference(Reference value) => PushUnchecked(value);

    #endregion

    #region Type-specific pops

    public int PopInt() => (int)Pop();

    public long PopLong() => Pop();

    public float PopFloat() => BitConverter.Int32BitsToSingle((int)Pop());

    public double PopDouble() => BitConverter.Int64BitsToDouble(Pop());

    public bool PopBool() => Pop() != 0;

    public sbyte PopByte() => (sbyte)Pop();

    public short PopShort() => (short)Pop();

    public char PopChar() => (char)Pop();

    public Reference PopReference() => Pop();

    #endregion

    #region Reverse pop methods

    private int _from;

    public void SetFrom(int offset) => _from = StackTop - offset;

    public long PopUnknownFrom()
    {
        var v = Stack[_from];
        _from++;
        return v;
    }

    public int PopIntFrom()
    {
        var v = Stack[_from];
        _from++;
        return (int)v;
    }

    public long PopLongFrom()
    {
        var v = Stack[_from];
        _from++;
        return v;
    }

    public float PopFloatFrom()
    {
        var v = Stack[_from];
        _from++;
        return BitConverter.Int32BitsToSingle((int)v);
    }

    public double PopDoubleFrom()
    {
        var v = Stack[_from];
        _from++;
        return BitConverter.Int64BitsToDouble(v);
    }

    public bool PopBoolFrom()
    {
        var v = Stack[_from];
        _from++;
        return v != 0;
    }

    public sbyte PopByteFrom()
    {
        var v = Stack[_from];
        _from++;
        return (sbyte)v;
    }

    public short PopShortFrom()
    {
        var v = Stack[_from];
        _from++;
        return (short)v;
    }

    public char PopCharFrom()
    {
        var v = Stack[_from];
        _from++;
        return (char)v;
    }

    public Reference PopReferenceFrom()
    {
        var v = Stack[_from];
        _from++;
        return v;
    }

    public void Discard(int count)
    {
        StackTop -= count;
    }

    #endregion

    #region Locals

    /// <summary>
    ///     Pops a value from stack and stores into local variable.
    /// </summary>
    /// <param name="index">Local index.</param>
    public void PopToLocal(int index)
    {
#if DEBUG
        if (StackTop == 0)
            throw new JavaRuntimeError($"Stack underflow in {Method.Method}");
#endif
        StackTop--;
        LocalVariables[index] = Stack[StackTop];
    }

    /// <summary>
    ///     Pushes value from local variables. No checks are performed.
    /// </summary>
    /// <param name="index">Index of local variable.</param>
    public void PushFromLocal(int index) => PushUnchecked(LocalVariables[index]);

    #endregion

    public override string ToString()
    {
        return $"{Method}:{Pointer}";
    }
}