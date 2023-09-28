namespace MahoTrans.Runtime;

public class Frame
{
    public JavaMethodBody Method;
    public int Pointer;
    public long[] LocalVariables;
    private long[] _stack;

    /// <summary>
    /// True means that entry is double-sized.
    /// </summary>
    public bool[] StackSizes;

    public int StackTop;

    public Frame(JavaMethodBody method)
    {
        Method = method;
        LocalVariables = new long[method.LocalsCount];
        _stack = new long[method.StackSize];
        StackSizes = new bool[method.StackSize];
    }

    public void Reinitialize(JavaMethodBody method)
    {
        StackTop = 0;
        Pointer = 0;
        if (Method != method)
        {
            Method = method;
            _stack = new long[method.StackSize];
            StackSizes = new bool[method.StackSize];
            LocalVariables = new long[method.LocalsCount];
        }
    }

    public void DiscardAll() => StackTop = 0;

    #region Basic stack operations

    /// <summary>
    /// Pushes value into the stack.
    /// </summary>
    /// <param name="value">Value to push.</param>
    /// <param name="size">True if value is long or double.</param>
    public void PushUnchecked(long value, bool size)
    {
        _stack[StackTop] = value;
        StackSizes[StackTop] = size;
        StackTop++;
    }

    public long Pop()
    {
        StackTop--;
        return _stack[StackTop];
    }

    public bool IsDoubleSizedPopped()
    {
        return StackSizes[StackTop];
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

    public void PushInt(int value) => PushUnchecked(value, false);

    public void PushLong(long value) => PushUnchecked(value, true);

    public void PushFloat(float value) => PushUnchecked(BitConverter.SingleToInt32Bits(value), false);

    public void PushDouble(double value) => PushUnchecked(BitConverter.DoubleToInt64Bits(value), true);

    public void PushBool(bool value) => PushUnchecked(value ? 1L : 0L, false);

    public void PushByte(sbyte value) => PushUnchecked(value, false);

    public void PushShort(short value) => PushUnchecked(value, false);

    public void PushChar(char value) => PushUnchecked(value, false);

    public void PushReference(Reference value) => PushUnchecked(value, false);

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
        var v = _stack[_from];
        _from++;
        return v;
    }

    public int PopIntFrom()
    {
        var v = _stack[_from];
        _from++;
        return (int)v;
    }

    public long PopLongFrom()
    {
        var v = _stack[_from];
        _from++;
        return v;
    }

    public float PopFloatFrom()
    {
        var v = _stack[_from];
        _from++;
        return BitConverter.Int32BitsToSingle((int)v);
    }

    public double PopDoubleFrom()
    {
        var v = _stack[_from];
        _from++;
        return BitConverter.Int64BitsToDouble(v);
    }

    public bool PopBoolFrom()
    {
        var v = _stack[_from];
        _from++;
        return v != 0;
    }

    public sbyte PopByteFrom()
    {
        var v = _stack[_from];
        _from++;
        return (sbyte)v;
    }

    public short PopShortFrom()
    {
        var v = _stack[_from];
        _from++;
        return (short)v;
    }

    public char PopCharFrom()
    {
        var v = _stack[_from];
        _from++;
        return (char)v;
    }

    public Reference PopReferenceFrom()
    {
        var v = _stack[_from];
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
    /// Pops a value from stack, checks its type and stores into local variable.
    /// </summary>
    /// <param name="index">Local index.</param>
    public void PopToLocal(int index)
    {
        var v = Pop();
        LocalVariables[index] = v;
    }

    public void PushFromLocal(int index, bool size) => PushUnchecked(LocalVariables[index], size);

    #endregion

    public override string ToString()
    {
        return $"{Method}:{Pointer}";
    }
}