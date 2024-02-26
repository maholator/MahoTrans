// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

// ReSharper disable MemberCanBePrivate.Global

namespace MahoTrans.Builder;

/// <summary>
///     Utility for building JVM bytecode pieces.
/// </summary>
public class JavaMethodBuilder
{
    private readonly JavaClass _class;

    private readonly List<IBuilderEntry> _code = new();
    private readonly Dictionary<int, int> _labels = new();

    private readonly Dictionary<int, int> _loopStates = new();

    private readonly List<JavaTryCatch> _tryCatches = new();

    /// <summary>
    ///     Creates a builder.
    /// </summary>
    /// <param name="cls">
    ///     Class, where the method will be placed. Usually, you get it in arguments of non-native method
    ///     builder.
    /// </param>
    public JavaMethodBuilder(JavaClass cls)
    {
        _class = cls;
    }

    public void Append(JavaOpcode opcode)
    {
        Append(new Instruction(opcode));
    }

    public void Append(JavaOpcode opcode, byte arg)
    {
        Append(new Instruction(opcode, arg));
    }

    public void Append(Instruction instruction)
    {
        _code.Add(new InstructionEntry(instruction));
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
        AppendVirtcall(name, $"(){returns.ToJavaDescriptor()}");
    }

    public void AppendVirtcall(string name, Type returns, params Type[] args)
    {
        var descriptor = DescriptorUtils.BuildMethodDescriptor(returns, args);
        AppendVirtcall(name, descriptor);
    }

    public void AppendStaticCall(NameDescriptorClass nameDescriptor)
    {
        var c = _class.PushConstant(nameDescriptor).Split();
        Append(new Instruction(JavaOpcode.invokestatic, c));
    }

    /// <summary>
    ///     Calls a static method.
    /// </summary>
    /// <param name="name">Method's name.</param>
    /// <param name="returns">Return type.</param>
    /// <param name="args">Types of the parameters.</param>
    /// <typeparam name="T">Class, where the static method is hosted.</typeparam>
    public void AppendStaticCall<T>(string name, Type returns, params Type[] args) where T : Object
    {
        var descriptor = DescriptorUtils.BuildMethodDescriptor(returns, args);
        AppendStaticCall(new NameDescriptorClass(name, descriptor, typeof(T)));
    }

    /// <summary>
    ///     Gets field from type, where the method is creating.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    public void AppendGetLocalField(string name, string type)
    {
        var c = _class.PushConstant(new NameDescriptorClass(name, type, _class.Name)).Split();
        Append(new Instruction(JavaOpcode.getfield, c));
    }

    /// <summary>
    ///     Gets field from type, where the method is creating.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    public void AppendGetLocalField(string name, Type type) => AppendGetLocalField(name, type.ToJavaDescriptor());

    /// <summary>
    ///     Gets field from arbitrary type.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="cls">Type that contains the field.</param>
    public void AppendGetField(string name, string type, Type cls)
    {
        var c = _class.PushConstant(new NameDescriptorClass(name, type, cls)).Split();
        Append(new Instruction(JavaOpcode.getfield, c));
    }

    /// <summary>
    ///     Gets field from arbitrary type.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="cls">Type that contains the field.</param>
    public void AppendGetField(string name, Type type, Type cls) =>
        AppendGetField(name, type.ToJavaDescriptor(), cls);

    /// <summary>
    ///     Puts field to arbitrary type.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="cls">Type that contains the field.</param>
    public void AppendPutField(string name, string type, Type cls)
    {
        var c = _class.PushConstant(new NameDescriptorClass(name, type, cls)).Split();
        Append(new Instruction(JavaOpcode.putfield, c));
    }

    /// <summary>
    ///     Puts field to arbitrary type.
    /// </summary>
    /// <param name="name">Name of the field.</param>
    /// <param name="type">Type of the field.</param>
    /// <param name="cls">Type that contains the field.</param>
    public void AppendPutField(string name, Type type, Type cls) =>
        AppendPutField(name, type.ToJavaDescriptor(), cls);

    public void AppendNewObject<T>() where T : Object
    {
        var c = _class.PushConstant(typeof(T).ToJavaName()).Split();
        Append(new Instruction(JavaOpcode.newobject, c));
    }

    public void AppendInt(int i)
    {
        var c = _class.PushConstant(i).Split();
        Append(new Instruction(JavaOpcode.ldc_w, c));
    }

    public void AppendConstant(string str)
    {
        var c = _class.PushConstant(str).Split();
        Append(new Instruction(JavaOpcode.ldc_w, c));
    }

    public void AppendConstant(long l)
    {
        var c = _class.PushConstant(l).Split();
        Append(new Instruction(JavaOpcode.ldc2_w, c));
    }

    public void AppendConstant(double d)
    {
        var c = _class.PushConstant(d).Split();
        Append(new Instruction(JavaOpcode.ldc2_w, c));
    }

    #region Labels and jumps

    /// <summary>
    ///     Creates a label in this method. Places it at the current end of the method.
    /// </summary>
    /// <returns>Label handle. You can move it to better place.</returns>
    public JavaLabel PlaceLabel()
    {
        return new JavaLabel(this, _labels.Push(_code.Count));
    }

    /// <summary>
    ///     Moves already defined label to the current end of the method.
    /// </summary>
    /// <param name="label">Label to move.</param>
    public void BringLabel(JavaLabel label)
    {
        _labels[label] = _code.Count;
    }

    #endregion

    #region Loops

    /// <summary>
    ///     Begins while(condition) { body } loop. Append your body first, then call <see cref="BeginLoopCondition" /> then
    ///     append the condition.
    /// </summary>
    /// <param name="condition">
    ///     Goto opcode to go to loop's beginning.
    ///     So, if your condition is i &lt; length, you should use <see cref="JavaOpcode.if_icmplt" />. In condition section,
    ///     push to stack i then length.
    /// </param>
    /// <returns>
    ///     Loop handle. Call <see cref="BeginLoopCondition" /> to mark condition start. Call <see cref="EndLoop" /> to end the
    ///     loop.
    /// </returns>
    /// <remarks>
    ///     Wrap this in using block to make it look like real code block. Loop end will be managed automatically.
    /// </remarks>
    public JavaLoop BeginLoop(JavaOpcode condition)
    {
        var id = _loopStates.Push(1);
        var lc = AppendGoto();
        var lb = PlaceLabel();
        return new JavaLoop(this, id, lb, lc, condition);
    }

    /// <summary>
    ///     Begins try block. Append your try block body, then call <see cref="JavaTryCatch.CatchSection" />, then append catch
    ///     block body. This must be used in using block. Catch end will be managed automatically.
    /// </summary>
    /// <typeparam name="T">Exception type to catch.</typeparam>
    /// <returns>Try-catch handle to pass to using statement.</returns>
    public JavaTryCatch BeginTry<T>() where T : Throwable
    {
        return BeginTry(typeof(T).ToJavaName());
    }

    /// <summary>
    ///     Begins try block. Append your try block body, then call <see cref="JavaTryCatch.CatchSection" />, then append catch
    ///     block body. This must be used in using block. Catch end will be managed automatically.
    /// </summary>
    /// <param name="exceptionName">Class name of the exception to catch.</param>
    /// <returns>Try-catch handle to pass to using statement.</returns>
    public JavaTryCatch BeginTry(string exceptionName)
    {
        var ex = _class.PushConstant(exceptionName);
        var tryBegin = PlaceLabel();
        var catchBegin = PlaceLabel();
        var catchEnd = PlaceLabel();
        var tc = new JavaTryCatch(this, ex, tryBegin, catchBegin, catchEnd);
        _tryCatches.Add(tc);
        return tc;
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

    public bool LastOpcodePerformsJump
    {
        get
        {
            if (_code.Count == 0)
                return false;

            switch (_code[^1])
            {
                case GotoEntry gotoEntry:
                    return gotoEntry.Opcode.IsJumpOpcode();
                case InstructionEntry instructionEntry:
                    return instructionEntry.Instruction.Opcode.IsJumpOpcode();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public Instruction[] BuildCode()
    {
        var offsets = CalculateOffsets();

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

    public JavaMethodBody.Catch[] BuildTryCatches()
    {
        var offsets = CalculateOffsets();

        var result = new JavaMethodBody.Catch[_tryCatches.Count];
        for (int i = 0; i < _tryCatches.Count; i++)
        {
            var tc = _tryCatches[i];
            // [start; end)

            ushort tryStart = (ushort)offsets[_labels[tc.TryBegin]];
            ushort catchStart = (ushort)offsets[_labels[tc.CatchBegin]];

            result[i] = new JavaMethodBody.Catch(tryStart, catchStart, catchStart, (ushort)tc.Exception);
        }

        return result;
    }

    public JavaMethodBody Build(int maxStack, int maxLocals)
    {
        return new JavaMethodBody(maxStack, maxLocals)
        {
            Code = BuildCode(),
            Catches = BuildTryCatches(),
        };
    }

    private int[] CalculateOffsets()
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

        return offsets;
    }
}