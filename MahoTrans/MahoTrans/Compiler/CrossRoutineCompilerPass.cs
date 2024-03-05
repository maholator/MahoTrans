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
public class CrossRoutineCompilerPass
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
        if (_ccrfr.StackOnEnter.HasValue)
        {
            // let's build our entrance...
            performPop(_ccrfr.StackOnEnter.Value, StackPurposes[0][0], 0);
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
                    PushConstant(instr);
                    break;
                case OpcodeType.Local:
                    break;
                case OpcodeType.Array:
                    break;
                case OpcodeType.Stack:
                    break;
                case OpcodeType.Math:
                    break;
                case OpcodeType.Conversion:
                    break;
                case OpcodeType.Compare:
                    break;
                case OpcodeType.Branch:
                    break;
                case OpcodeType.Jump:
                    break;
                case OpcodeType.Return:
                    break;
                case OpcodeType.Throw:
                    break;
                case OpcodeType.Call:
                    break;
                case OpcodeType.VirtCall:
                    break;
                case OpcodeType.Static:
                    break;
                case OpcodeType.Alloc:
                    break;
                case OpcodeType.Monitor:
                    break;
                case OpcodeType.Cast:
                    break;
                case OpcodeType.Bridge:
                    break;
                case OpcodeType.Initializer:
                    break;
                case OpcodeType.Error:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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

    public void PushConstant(LinkedInstruction instr)
    {
        using (new MarshallerWrapper(this, ^1))
        {
            switch (instr.Opcode)
            {
                case MTOpcode.iconst_m1:
                    _il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case MTOpcode.iconst_0:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case MTOpcode.iconst_1:
                    _il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case MTOpcode.iconst_2:
                    _il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case MTOpcode.iconst_3:
                    _il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case MTOpcode.iconst_4:
                    _il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case MTOpcode.iconst_5:
                    _il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case MTOpcode.lconst_0:
                    _il.Emit(OpCodes.Ldc_I8, 0L);
                    break;
                case MTOpcode.lconst_1:
                    _il.Emit(OpCodes.Ldc_I8, 1L);
                    break;
                case MTOpcode.lconst_2:
                    _il.Emit(OpCodes.Ldc_I8, 2L);
                    break;
                case MTOpcode.fconst_0:
                    _il.Emit(OpCodes.Ldc_R4, 0F);
                    break;
                case MTOpcode.fconst_1:
                    _il.Emit(OpCodes.Ldc_R4, 1F);
                    break;
                case MTOpcode.fconst_2:
                    _il.Emit(OpCodes.Ldc_R4, 2F);
                    break;
                case MTOpcode.dconst_0:
                    _il.Emit(OpCodes.Ldc_R8, 0D);
                    break;
                case MTOpcode.dconst_1:
                    _il.Emit(OpCodes.Ldc_R8, 1D);
                    break;
                case MTOpcode.dconst_2:
                    _il.Emit(OpCodes.Ldc_R8, 2D);
                    break;
                case MTOpcode.iconst:
                    _il.Emit(OpCodes.Ldc_I4, instr.IntData);
                    break;
                case MTOpcode.strconst:
                    _il.Emit(OpCodes.Ldstr, (string)instr.Data);
                    break;
                case MTOpcode.lconst:
                    _il.Emit(OpCodes.Ldc_I8, (long)instr.Data);
                    break;
                case MTOpcode.dconst:
                    _il.Emit(OpCodes.Ldc_R8, (double)instr.Data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void performPop(PrimitiveType primitive, StackValuePurpose purp, int stackPos)
    {
        using (new MarshallerWrapper(this, primitive, purp, stackPos))
        {
            // frame on stage!
            _il.Emit(OpCodes.Ldarg_0);

            // now let's pop a value.
            switch (purp)
            {
                default:
                    _il.Emit(OpCodes.Call, StackPopMethods[primitive.ToType()]);
                    break;
                case StackValuePurpose.MethodArg:
                case StackValuePurpose.FieldValue:
                    var real = GetExactType(stackPos);
                    var poppable = GetStackTypeFor(real);
                    _il.Emit(OpCodes.Call, StackPopMethods[poppable]);
                    break;
            }
        }
    }

    /// <summary>
    ///     Wrap push to CLR stack into usage of this struct.
    /// </summary>
    public struct MarshallerWrapper : IDisposable
    {
        private CrossRoutineCompilerPass _pass;
        private PrimitiveType _primitive;
        private StackValuePurpose _purp;
        private Index _stackPos;

        public MarshallerWrapper(CrossRoutineCompilerPass pass, PrimitiveType primitive, StackValuePurpose purp,
            Index stackPos)
        {
            _pass = pass;
            _primitive = primitive;
            _purp = purp;
            _stackPos = stackPos;
            before();
        }

        public MarshallerWrapper(CrossRoutineCompilerPass pass, Index stackPos)
        {
            _pass = pass;
            var currInstr = _pass._instrIndex;
            // looking into next instruction
            _purp = _pass.StackPurposes[currInstr + 1][stackPos];
            _primitive = _pass.StackTypes[currInstr + 1][stackPos];
            _stackPos = stackPos;
        }

        public void Dispose() => after();

        private void before()
        {
            switch (_purp)
            {
                case StackValuePurpose.ToLocal:
                case StackValuePurpose.ReturnToStack:
                    // to return to frame, we need a frame.
                    _pass._il.Emit(OpCodes.Ldarg_0);
                    // others need nothing.
                    break;
            }
        }

        private void after()
        {
            switch (_purp)
            {
                case StackValuePurpose.Consume:
                case StackValuePurpose.ToLocal:
                    // we need raw value.
                    break;
                case StackValuePurpose.ReturnToStack:
                    _pass._il.Emit(OpCodes.Call, StackPushMethods[_primitive.ToType()]);
                    break;
                case StackValuePurpose.Target:
                    // resolve object
                    _pass._il.Emit(OpCodes.Call, ResolveAnyObject);
                    break;
                default:
                    // this is an array. Resolve an array.
                    _pass._il.Emit(OpCodes.Call, ResolveArrEx.MakeGenericMethod(_purp.ToArrayType()));
                    break;
                case StackValuePurpose.MethodArg:
                case StackValuePurpose.FieldValue:
                    // we need EXACT value. Applying marshaller.
                    var real = _pass.GetExactType(
                        _stackPos.GetOffset(_pass.StackPurposes[_pass._instrIndex + 1].Length));
                    var poppable = GetStackTypeFor(real);
                    var marshaller = MarshalUtils.GetMarshallerFor(poppable, false, real, false);
                    if (marshaller != null)
                        _pass._il.Emit(OpCodes.Call, marshaller);
                    break;
            }
        }
    }
}