// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MahoTrans.Runtime;

public unsafe class Frame
{
    public JavaMethodBody Method;
    public int Pointer;
    public long* LocalVariables;
    public long* Stack = null;

    /// <summary>
    ///     Pointer to stack top. Stack contains topmost operand at this index-1 (if zero, stack is empty). Stack length is
    ///     equal to this field.
    /// </summary>
    public int StackTop;

    public Frame(JavaMethodBody method)
    {
        Method = method;
        AllocateBuffers(method.LocalsCount, method.StackSize);
    }

    public void Reinitialize(JavaMethodBody method)
    {
        StackTop = 0;
        Pointer = 0;

        if (Method == method)
        {
            Unsafe.InitBlock(Stack, 0, (uint)(method.StackSize * sizeof(long)));
            Unsafe.InitBlock(LocalVariables, 0, (uint)(method.LocalsCount * sizeof(long)));
        }
        else
        {
            Method = method;
            DeallocateBuffers();
            AllocateBuffers(method.LocalsCount, method.StackSize);
        }
    }

    private void AllocateBuffers(ushort locals, ushort stack)
    {
        Stack = (long*)NativeMemory.Alloc(stack, sizeof(long));
        Unsafe.InitBlock(Stack, 0, (uint)(stack * sizeof(long)));
        LocalVariables = (long*)NativeMemory.Alloc(locals, sizeof(long));
        Unsafe.InitBlock(LocalVariables, 0, (uint)(locals * sizeof(long)));
    }

    private void DeallocateBuffers()
    {
        if (Stack == null)
            return;

        NativeMemory.Free(Stack);
        NativeMemory.Free(LocalVariables);
    }

    ~Frame()
    {
        DeallocateBuffers();
    }

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

    public void Push(object value)
    {
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
        var v = Pop();
        LocalVariables[index] = v;
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