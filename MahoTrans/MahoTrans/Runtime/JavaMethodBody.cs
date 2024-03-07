// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime;

public class JavaMethodBody
{
    /// <summary>
    ///     Method that this body belongs to.
    /// </summary>
    public Method Method = null!;

    #region Data from class file

    public ushort StackSize;

    public ushort LocalsCount;

    /// <summary>
    ///     Bytecode of this method. If your bytecode has no calculated offsets, use <see cref="RawCode" /> instead.
    /// </summary>
    public Instruction[] Code = Array.Empty<Instruction>();

    public Catch[] Catches = Array.Empty<Catch>();

    public JavaAttribute[] RawAttributes = Array.Empty<JavaAttribute>();

    #endregion

    #region Data calculated by linker

    /// <summary>
    ///     Linked bytecode of this method. This method must be linked.
    /// </summary>
    public LinkedInstruction[] LinkedCode = null!;

    /// <summary>
    ///     Linked exceptions table of this method. This method must be linked.
    /// </summary>
    public LinkedCatch[] LinkedCatches = null!;

    /// <summary>
    ///     Types of local variables. This method must be linked.
    /// </summary>
    public PrimitiveType[] LocalTypes = null!;

    /// <summary>
    ///     Types of values on stack. This method must be linked. This reflects
    ///     stack state BEFORE opcode execution.
    /// </summary>
    public PredictedStackState[] StackTypes = null!;

    /// <summary>
    ///     Sizes of arguments. Array size is equal to args count WITH "this" arg. Each size is one, if argument is 32-bit and
    ///     two if 64-bit. This method must be linked.
    /// </summary>
    /// <example>
    ///     For static method (IJI)V: 1,2,1 <br />
    ///     For instance method (IFD)V: 1,1,1,2 (first "1" is "this" arg, last "2" is the third argument - double)
    /// </example>
    public byte[] ArgsSizes = null!;

    /// <summary>
    ///     Slot for everything that may be helpful for other subsystems but not needed in runtime. This is filled by linker.
    ///     This may contain references to methods, fields or other objects.
    /// </summary>
    public object?[] AuxiliaryLinkerOutput = null!;

    #endregion

    public JavaMethodBody()
    {
    }

    public JavaMethodBody(int stack, int locals)
    {
        StackSize = (ushort)stack;
        LocalsCount = (ushort)locals;
    }

    /// <summary>
    ///     Assign bytecode using this property to automatically calculate offsets.
    /// </summary>
    public Instruction[] RawCode
    {
        set
        {
            int offset = 0;
            Instruction[] copy = new Instruction[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                var instruction = value[i];
                copy[i] = new Instruction(offset, instruction.Opcode, instruction.Args);
                offset++;
                offset += instruction.Args.Length;
            }

            Code = copy;
        }
    }

    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public override string ToString() => Method?.ToString() ?? "Detached";

    public struct Catch
    {
        public ushort TryStart;
        public ushort TryEnd;
        public ushort CatchStart;
        public ushort Type;

        public Catch(ushort tryStart, ushort tryEnd, ushort catchStart, ushort type)
        {
            TryStart = tryStart;
            TryEnd = tryEnd;
            CatchStart = catchStart;
            Type = type;
        }

        public bool IsIn(Instruction instruction)
        {
            // [start; end)
            if (instruction.Offset < TryStart)
                return false;
            if (instruction.Offset >= TryEnd)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int trys = (TryStart << 16) | TryEnd;
            return trys ^ CatchStart;
        }
    }

    /// <summary>
    ///     <see cref="Catch" />, but precalculated.
    /// </summary>
    public struct LinkedCatch
    {
        /// <summary>
        ///     Index of first instruction, covered with this try. Inclusive.
        /// </summary>
        public ushort TryStart;

        /// <summary>
        ///     Index of last instruction, covered with this try. Exclusive.
        /// </summary>
        public ushort TryEnd;

        /// <summary>
        ///     Index of instruction to jump to get into catch block.
        /// </summary>
        public ushort CatchStart;

        /// <summary>
        ///     Exception type. Always non-null.
        /// </summary>
        public JavaClass ExceptionType;

        public LinkedCatch(ushort tryStart, ushort tryEnd, ushort catchStart, JavaClass exceptionType)
        {
            TryStart = tryStart;
            TryEnd = tryEnd;
            CatchStart = catchStart;
            ExceptionType = exceptionType;
        }

        public LinkedCatch(int tryStart, int tryEnd, int catchStart, JavaClass exceptionType)
        {
            TryStart = (ushort)tryStart;
            TryEnd = (ushort)tryEnd;
            CatchStart = (ushort)catchStart;
            ExceptionType = exceptionType;
        }

        public bool IsIn(int index)
        {
            return index >= TryStart && index <= TryEnd;
        }
    }

    public uint GetSnapshotHash()
    {
        uint acc = 0;
        foreach (var instruction in Code)
        {
            acc ^= (uint)instruction.GetHashCode();
        }

        foreach (var @catch in Catches)
        {
            acc ^= (uint)@catch.GetHashCode();
        }

        return acc ^ ((uint)(StackSize << 16) | LocalsCount);
    }
}
