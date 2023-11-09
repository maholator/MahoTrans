using System.Text;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

// ReSharper disable MemberCanBePrivate.Global

namespace MahoTrans.Builder;

public class JavaMethodBuilder
{
    private readonly JavaClass _class;

    private readonly List<IBuilderEntry> _code = new();
    private readonly Dictionary<int, int> _labels = new();

    private readonly Dictionary<int, int> _loopStates = new();


    public JavaMethodBuilder(JavaClass cls)
    {
        _class = cls;
    }

    public void Append(JavaOpcode opcode)
    {
        Append(new Instruction(opcode));
    }

    public void Append(params JavaOpcode[] opcodes)
    {
        foreach (var opcode in opcodes)
        {
            Append(opcode);
        }
    }

    public void Append(Instruction instruction)
    {
        _code.Add(new InstructionEntry(instruction));
    }

    public void Append(params Instruction[] code)
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

    public JavaLabel AppendGoto(JavaOpcode opcode = JavaOpcode.@goto)
    {
        var l = PlaceLabel();
        AppendGoto(opcode, l);
        return l;
    }

    public void AppendVirtcall(NameDescriptor nameDescriptor)
    {
        var c = _class.PushConstant(nameDescriptor).Split();
        Append(new Instruction(JavaOpcode.invokevirtual, c));
    }

    public void AppendVirtcall(string name, string descriptor)
    {
        AppendVirtcall(new NameDescriptor(name, descriptor));
    }

    public void AppendVirtcall(string name, Type returns)
    {
        AppendVirtcall(name, $"(){returns.ToJavaDescriptorNative()}");
    }

    public void AppendVirtcall(string name, Type returns, params Type[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('(');
        foreach (var type in args)
        {
            sb.Append(type.ToJavaDescriptorNative());
        }

        sb.Append(')');
        sb.Append(returns.ToJavaDescriptorNative());
        AppendVirtcall(name, sb.ToString());
    }

    public void AppendGetLocalField(string name, string type)
    {
        var c = _class.PushConstant(new NameDescriptorClass(name, type, _class.Name)).Split();
        Append(new Instruction(JavaOpcode.getfield, c));
    }

    public void AppendGetLocalField(string name, Type type) => AppendGetLocalField(name, type.ToJavaDescriptorNative());

    #region Labels and jumps

    public JavaLabel PlaceLabel()
    {
        return new JavaLabel(this, _labels.Push(_code.Count, 1));
    }

    public void BringLabel(JavaLabel label)
    {
        _labels[label] = _code.Count;
    }

    #endregion

    #region Loops

    public JavaLoop BeginLoop(JavaOpcode condition)
    {
        var id = _loopStates.Push(1, 1);
        var lc = AppendGoto();
        var lb = PlaceLabel();
        return new JavaLoop(this, id, lb, lc, condition);
    }

    public void BeginLoopCondition(JavaLoop loop)
    {
        if (_loopStates[loop.Number] == 1)
            _loopStates[loop.Number] = 2;
        else
            throw new InvalidOperationException("Loop is in invalid state");

        BringLabel(loop.ConditionBegin);
    }

    public void EndLoop(JavaLoop loop)
    {
        if (_loopStates[loop.Number] == 2)
            _loopStates[loop.Number] = 3;
        else
            throw new InvalidOperationException("Loop is in invalid state");
        AppendGoto(loop.Condition, loop.LoopBegin);
    }

    #endregion

    public Instruction[] Build()
    {
        var len = 0;
        var offsets = new int[_code.Count];
        for (var i = 0; i < _code.Count; i++)
        {
            var entry = _code[i];
            offsets[i] = len;
            len++;
            len += entry.ArgsLength;
        }

        var code = new Instruction[_code.Count];
        for (var i = 0; i < _code.Count; i++)
        {
            var entry = _code[i];
            var thisOffset = offsets[i];
            switch (entry)
            {
                case GotoEntry gotoEntry:
                {
                    var targetOffset = offsets[_labels[gotoEntry.Label]];
                    var jump = targetOffset - thisOffset;
                    code[i] = new Instruction(thisOffset, gotoEntry.Opcode, jump.Split());
                    break;
                }
                case InstructionEntry ie:
                    code[i] = new Instruction(thisOffset, ie.Instruction.Opcode, ie.Instruction.Args);
                    break;
                default:
                    throw new JavaLinkageException("Invalid builder entry");
            }
        }

        return code;
    }

    public JavaMethodBody Build(int maxStack, int maxLocals)
    {
        return new JavaMethodBody(maxStack, maxLocals)
        {
            Code = Build()
        };
    }
}