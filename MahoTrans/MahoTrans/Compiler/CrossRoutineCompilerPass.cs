// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using static MahoTrans.Compiler.CompilerUtils;

namespace MahoTrans.Compiler;

/// <summary>
///     State object for single cross-compiler pass.
/// </summary>
public partial class CrossRoutineCompilerPass
{
    private readonly JavaMethodBody _javaBody;

    private readonly CCRFR _ccrfr;
    private readonly TypeBuilder _host;
    private readonly string _methodName;

    /// <summary>
    ///     Stack map. Length of this is range.length+1. Element at index I represents stack before instruction with index
    ///     I in the range. The last element is stack state after last instruction (and before CCR exit). Each element is array
    ///     of purposes for each value. Indexing from zero. Array lengths are equal to stack sizes.
    /// </summary>
    public readonly StackValuePurpose[][] StackPurposes;

    /// <summary>
    ///     Stack map. Length of this is range.length+1. Element at index I represents stack before instruction with index
    ///     I in the range. The last element is stack state after last instruction (and before CCR exit). Each element is array
    ///     of each value "java primitive" types. Indexing from zero. Array lengths are equal to stack sizes.
    /// </summary>
    public readonly PrimitiveType[][] StackTypes;

    public readonly Dictionary<int, int> StackObjectPushMap;

    public LinkedInstruction[] JavaCode => _javaBody.LinkedCode;

    public int JavaCodeLength => JavaCode.Length;

    private ILGenerator _il = null!;

    /// <summary>
    ///     Index of instruction we are working on.
    /// </summary>
    private int _instrIndex;

    public CrossRoutineCompilerPass(JavaMethodBody javaBody, CCRFR ccrfr, TypeBuilder host, string methodName)
    {
        _javaBody = javaBody;
        _ccrfr = ccrfr;
        _host = host;
        _methodName = methodName;
        StackPurposes = CrossCompilerUtils.PredictPurposes(javaBody, ccrfr);
        StackTypes = CrossCompilerUtils.PredictTypes(javaBody, ccrfr);
        StackObjectPushMap = CrossCompilerUtils.PredictStackObject(StackPurposes);
    }

    public MethodBuilder Compile()
    {
        var mb = _host.DefineMethod(_methodName,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Final, CallingConventions.Standard,
            null, new[] { typeof(Frame) });
        mb.DefineParameter(1, ParameterAttributes.None, "javaFrame");

        lock (this)
        {
            _il = mb.GetILGenerator();
            CompileInternal();
        }

        return mb;
    }

    private void CompileInternal()
    {
        _instrIndex = _ccrfr.Start - 1;
        if (_ccrfr.StackOnEnter.HasValue)
        {
            // let's build our entrance: we need a value from jvm stack.
            using (BeginMarshalSection(^1))
            {
                // frame on stage!
                _il.Emit(OpCodes.Ldarg_0);

                // now let's pop a value.
                switch (StackPurposes[0][0])
                {
                    default:
                        _il.Emit(OpCodes.Call, StackPopMethods[_ccrfr.StackOnEnter.Value.ToType()]);
                        break;
                    case StackValuePurpose.MethodArg:
                    case StackValuePurpose.FieldValue:
                        var poppable = GetStackTypeFor(GetExactType(0));
                        _il.Emit(OpCodes.Call, StackPopMethods[poppable]);
                        break;
                }
            }
        }

        for (_instrIndex = _ccrfr.Start; _instrIndex < _ccrfr.EndExclusive; _instrIndex++)
        {
            var instr = JavaCode[_instrIndex];
            var opcode = instr.Opcode;
            switch (opcode.GetOpcodeType())
            {
                case OpcodeType.NoOp:
                    // do nothing
                    break;
                case OpcodeType.Constant:
                    CrossConstant(instr);
                    break;
                case OpcodeType.Local:
                    CrossLocal(instr);
                    break;
                case OpcodeType.Array:
                    CrossArray(instr);
                    break;
                case OpcodeType.Stack:
                    CrossStack(instr);
                    break;
                case OpcodeType.Math:
                    CrossMath(instr);
                    break;
                case OpcodeType.Conversion:
                    throw new NotImplementedException();
                case OpcodeType.Compare:
                    throw new NotImplementedException();
                case OpcodeType.Branch:
                    throw new NotImplementedException();
                case OpcodeType.Jump:
                    throw new NotImplementedException();
                case OpcodeType.Return:
                    throw new NotImplementedException();
                case OpcodeType.Throw:
                    CrossThrow(instr);
                    break;
                case OpcodeType.Call:
                    throw new NotImplementedException();
                case OpcodeType.VirtCall:
                    throw new NotImplementedException();
                case OpcodeType.Static:
                    throw new NotImplementedException();
                case OpcodeType.Alloc:
                    throw new NotImplementedException();
                case OpcodeType.Monitor:
                    throw new NotImplementedException();
                case OpcodeType.Cast:
                    throw new NotImplementedException();
                case OpcodeType.Bridge:
                    throw new NotImplementedException();
                case OpcodeType.Initializer:
                    throw new NotImplementedException();
                case OpcodeType.Error:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // all returned values were returned.
        _il.Emit(OpCodes.Ret);
    }

    /// <summary>
    ///     Looks for type that must be on stack. Starts from next instruction.
    /// </summary>
    /// <param name="stackPos">Position on stack from zero.</param>
    private Type GetExactType(int stackPos)
    {
        for (int i = _instrIndex + 1; i < JavaCodeLength; i++)
        {
            var aux = _javaBody.AuxiliaryLinkerOutput[i];
            if (aux != null)
            {
                var stackLen = _javaBody.StackTypes[i].StackBeforeExecution.Length;
                if (aux is Field f)
                {
                    if (stackPos == stackLen - 1)
                        return f.NativeField!.FieldType;
                }
                else if (aux is Method m)
                {
                    var arg = stackLen - m.ArgsCount - stackPos;
                    if (arg >= 0)
                    {
                        if (m.NativeBody != null)
                            return m.NativeBody.GetParameters()[arg].ParameterType;
                        return DescriptorUtils.ParseMethodDescriptorAsPrimitives(m.Descriptor.Descriptor).args[0]
                            .ToType();
                    }
                }
            }
        }

        throw new JavaLinkageException("Failed to find value consumer");
    }
}