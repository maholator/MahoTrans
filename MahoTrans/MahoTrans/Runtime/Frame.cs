namespace MahoTrans.Runtime;

public class Frame
{
    public JavaMethodBody Method;
    public int Pointer;
    public long[] LocalVariables;
    public long[] Stack;

    /// <summary>
    /// Types of values on stack
    /// </summary>
    public PrimitiveType[] StackTypes;

    /// <summary>
    /// Pointer to stack top. Stack contains topmost operand at this index. Stack length is this field +1.
    /// </summary>
    public int StackTop;

    public Frame(JavaMethodBody method)
    {
        Method = method;
        LocalVariables = new long[method.LocalsCount];
        Stack = new long[method.StackSize];
        StackTypes = new PrimitiveType[method.StackSize];
    }

    public void Reinitialize(JavaMethodBody method)
    {
        StackTop = 0;
        Pointer = 0;

        if (Method != method)
        {
            Method = method;
            Stack = new long[method.StackSize];
            StackTypes = new PrimitiveType[method.StackSize];
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
    public void PushUnchecked(long value, PrimitiveType type)
    {
        Stack[StackTop] = value;
        StackTypes[StackTop] = type;
        StackTop++;
    }

    public long Pop()
    {
        StackTop--;
        return Stack[StackTop];
    }

    public bool IsDoubleSizedPopped()
    {
        return (StackTypes[StackTop] & PrimitiveType.IsDouble) != 0;
    }

    public PrimitiveType GetPoppedType()
    {
        return StackTypes[StackTop];
    }

    /// <summary>
    /// For debugger. Checks, is operand on stack double sized.
    /// </summary>
    /// <param name="offset">Zero to check just popped operand. One to check actual stack top. Two and more to check deeper values.</param>
    /// <returns>Is the operand double sized.</returns>
    /// <remarks>
    /// For example, there are values "53", "52" and "51" on stack. To check 52's size, pass 2. To check 53's, pass 3.
    /// </remarks>
    public bool IsDoubleSizeAt(int offset)
    {
        return (StackTypes[StackTop - offset] & PrimitiveType.IsDouble) != 0;
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

    public void PushInt(int value) => PushUnchecked(value, PrimitiveType.Int);

    public void PushLong(long value) => PushUnchecked(value, PrimitiveType.Long);

    public void PushFloat(float value) => PushUnchecked(BitConverter.SingleToInt32Bits(value), PrimitiveType.Float);

    public void PushDouble(double value) => PushUnchecked(BitConverter.DoubleToInt64Bits(value), PrimitiveType.Double);

    public void PushBool(bool value) => PushUnchecked(value ? 1L : 0L, PrimitiveType.Int);

    public void PushByte(sbyte value) => PushUnchecked(value, PrimitiveType.Int);

    public void PushShort(short value) => PushUnchecked(value, PrimitiveType.Int);

    public void PushChar(char value) => PushUnchecked(value, PrimitiveType.Int);

    public void PushReference(Reference value) => PushUnchecked(value, PrimitiveType.Reference);

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
    /// Pops a value from stack and stores into local variable.
    /// </summary>
    /// <param name="index">Local index.</param>
    public void PopToLocal(int index)
    {
        var v = Pop();
        LocalVariables[index] = v;
    }

    /// <summary>
    /// Pushes value from local variables. No checks are performed.
    /// </summary>
    /// <param name="index">Index of local variable.</param>
    /// <param name="type">Type of the value.</param>
    public void PushFromLocal(int index, PrimitiveType type) => PushUnchecked(LocalVariables[index], type);

    #endregion

    public override string ToString()
    {
        return $"{Method}:{Pointer}";
    }
}