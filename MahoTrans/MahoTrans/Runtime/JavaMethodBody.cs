// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public class JavaMethodBody
{
    public Method Method = null!;
    public ushort StackSize;
    public ushort LocalsCount;

    /// <summary>
    ///     Bytecode of this method. If your bytecode has no calculated offsets, use <see cref="RawCode" /> instead.
    /// </summary>
    public Instruction[] Code = Array.Empty<Instruction>();

    public Catch[] Catches = Array.Empty<Catch>();

    public JavaAttribute[] Attrs = Array.Empty<JavaAttribute>();

    #region Caches

    /// <summary>
    ///     Linked bytecode of this method. Do not forget to call <see cref="EnsureBytecodeLinked" /> before usage!
    /// </summary>
    public LinkedInstruction[] LinkedCode = null!;

    /// <summary>
    ///     Types of local variables. This method must be verified.
    /// </summary>
    public PrimitiveType[] LocalTypes = null!;

    /// <summary>
    ///     Types of values on stack. Do not forget to call <see cref="EnsureBytecodeLinked" /> before usage! This reflects
    ///     stack state BEFORE opcode execution.
    /// </summary>
    public PrimitiveType[][] StackTypes = null!;

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

    /// <summary>
    ///     Ensures that method's bytecode is linked. Links if not. This must be called inside JVM context.
    /// </summary>
    public void EnsureBytecodeLinked()
    {
        if (LinkedCode != null!)
            return;
        Object.Jvm.Toolkit.Logger.LogDebug(DebugMessageCategory.Jit, $"{Method} will be linked");
        BytecodeLinker.Link(this, Object.Jvm);
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