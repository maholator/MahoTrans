using System.Text;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Builder;

public class JavaMethodBuilder
{
    public JavaClass Class { get; }

    private List<IBuilderEntry> _code = new();
    private Dictionary<int, int> labels = new();

    public JavaMethodBuilder(JavaClass cls)
    {
        Class = cls;
    }

    public void Append(JavaOpcode opcode)
    {
        Append(new Instruction(opcode));
    }

    public void Append(Instruction instruction)
    {
        _code.Add(new InstructionEntry(instruction));
    }

    public void Append(Instruction[] code)
    {
        foreach (var instruction in code)
        {
            Append(instruction);
        }
    }

    public void AppendGoto(JavaOpcode opcode, JavaLabel label)
    {
        _code.Add(new GotoEntry(opcode, label));
    }

    public JavaLabel AppendForwardGoto(JavaOpcode opcode)
    {
        var l = PlaceLabel();
        AppendGoto(opcode, l);
        return l;
    }

    public void AppendVirtcall(NameDescriptor nameDescriptor)
    {
        var c = Class.PushConstant(nameDescriptor).Split();
        Append(new Instruction(JavaOpcode.invokevirtual, c));
    }

    public void AppendVirtcall(string name, string descr)
    {
        AppendVirtcall(new NameDescriptor(name, descr));
    }

    public void AppendVirtcall(string name, Type returns)
    {
        AppendVirtcall(name, $"(){returns.ToJavaDescriptor()}");
    }

    public void AppendVirtcall(string name, Type returns, params Type[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('(');
        foreach (var type in args)
        {
            var n = NativeLinker.GetDescriptorForNativeType(type);
            n ??= type.ToJavaDescriptor();
            sb.Append(n);
        }

        sb.Append(')');
        var r = NativeLinker.GetDescriptorForNativeType(returns);
        r ??= returns.ToJavaDescriptor();
        sb.Append(r);
        AppendVirtcall(name, sb.ToString());
    }

    public void AppendLoop(Instruction[] body, Instruction[] loop, JavaOpcode condition)
    {
        var conditionBegin = AppendForwardGoto(JavaOpcode.@goto);
        var loopBegin = PlaceLabel();
        Append(body);
        BringLabel(conditionBegin);
        Append(loop);
        AppendGoto(condition, loopBegin);
    }

    public void AppendLoadThis()
    {
        Append(new Instruction(JavaOpcode.aload_0));
    }

    public void AppendInc(byte variable, sbyte value)
    {
        Append(new Instruction(JavaOpcode.iinc, new[] { variable, (byte)value }));
    }

    public JavaLabel PlaceLabel()
    {
        return labels.Push(_code.Count, 1);
    }

    public void BringLabel(JavaLabel label)
    {
        labels[label] = _code.Count;
    }

    public JavaLoop BeginLoop(JavaOpcode condition)
    {
        var lc = AppendForwardGoto(JavaOpcode.@goto);
        var lb = PlaceLabel();
        return new JavaLoop(lb, lc, condition);
    }

    public void BeginLoopCondition(JavaLoop loop)
    {
        BringLabel(loop.ConditionBegin);
    }

    public void EndLoop(JavaLoop loop)
    {
        AppendGoto(loop.Condition, loop.LoopBegin);
    }

    public Instruction[] Build()
    {
        int len = 0;
        int[] offsets = new int[_code.Count];
        for (var i = 0; i < _code.Count; i++)
        {
            var entry = _code[i];
            offsets[i] = len;
            len++;
            len += entry.ArgsLength;
        }

        Instruction[] code = new Instruction[_code.Count];
        for (var i = 0; i < _code.Count; i++)
        {
            var entry = _code[i];
            if (entry is GotoEntry gotoEntry)
            {
                var tOf = offsets[gotoEntry.Label];
                code[i] = new Instruction(offsets[i], gotoEntry.Opcode, (tOf - offsets[i]).Split());
            }
            else if (entry is InstructionEntry ie)
            {
                code[i] = new Instruction(offsets[i], ie.Instruction.Opcode, ie.Instruction.Args);
            }
            else
            {
                throw new JavaLinkageException("Invalid builder entry");
            }
        }

        return code;
    }
}