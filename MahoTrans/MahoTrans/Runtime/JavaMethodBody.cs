using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public class JavaMethodBody
{
    public Method Method = null!;
    public ushort StackSize;
    public ushort LocalsCount;

    /// <summary>
    /// Bytecode of this method. If your bytecode has no calculated offsets, use <see cref="RawCode"/> instead.
    /// </summary>
    public Instruction[] Code = Array.Empty<Instruction>();

    /// <summary>
    /// Linked bytecode of this method. Do not forget to call <see cref="EnsureBytecodeLinked"/> before usage!
    /// </summary>
    public LinkedInstruction[] LinkedCode = null!;

    public Catch[] Catches = Array.Empty<Catch>();
    public JavaAttribute[] Attrs = Array.Empty<JavaAttribute>();

    public JavaMethodBody()
    {
    }

    public JavaMethodBody(int stack, int locals)
    {
        StackSize = (ushort)stack;
        LocalsCount = (ushort)locals;
    }

    /// <summary>
    /// Assign bytecode using this property to automatically calculate offsets.
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
    /// Ensures that method's bytecode is linked. Links if not. This must be called inside JVM context.
    /// </summary>
    public void EnsureBytecodeLinked()
    {
        if (LinkedCode != null!)
            return;
        Object.Jvm.Toolkit.Logger.LogDebug(DebugMessageCategory.Jit, $"{Method} will be linked");
        LinkedCode = BytecodeLinker.Link(this, Object.Jvm, Code);
    }

    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public override string ToString() => Method?.ToString() ?? "Detached";

    public struct Catch
    {
        public short TryStart;
        public short TryEnd;
        public short CatchStart;
        public short Type;

        public Catch(short tryStart, short tryEnd, short catchStart, short type)
        {
            this.TryStart = tryStart;
            this.TryEnd = tryEnd;
            this.CatchStart = catchStart;
            this.Type = type;
        }

        public bool IsIn(Instruction instruction)
        {
            if (instruction.Offset < TryStart)
                return false;
            if (instruction.Offset >= TryEnd)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int trys = (TryStart << 16) | (ushort)TryEnd;
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