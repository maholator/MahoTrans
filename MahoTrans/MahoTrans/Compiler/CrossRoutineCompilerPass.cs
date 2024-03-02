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

    private StackValuePurpose[][] _stackPurposes;

    private ILGenerator _il = null!;

    private int _instrIndex = 0;

    public CrossRoutineCompilerPass(JavaMethodBody javaBody, CCRFR ccrfr, TypeBuilder host, string methodName)
    {
        _javaBody = javaBody;
        _ccrfr = ccrfr;
        _host = host;
        _methodName = methodName;
        _stackPurposes = CrossCompilerUtils.PredictPurposes(_javaBody, _ccrfr);
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
        if (_ccrfr.StackOnEnter)
        {
            // let's build our entrance...
            if (_stackPurposes[0][0] == StackValuePurpose.ReturnToStack)
            {
                // ummm!?
            }
            else
            {
            }
        }
    }


    private void performPop(PrimitiveType primitive, StackValuePurpose purp, int stackPos)
    {
        switch (purp)
        {
            case StackValuePurpose.Consume:
                // we need raw value
                break;
            case StackValuePurpose.Target:
            case StackValuePurpose.ArrayTarget:
                // let's resolve via extension for now
                break;
            case StackValuePurpose.MethodArg:
            case StackValuePurpose.FieldValue:
                // marshallers will be applied as is
                break;
            case StackValuePurpose.ToLocal:
            case StackValuePurpose.ReturnToStack:
                // well, we need a frame...
                _il.Emit(OpCodes.Ldarg_0);
                break;
        }

        // frame on stage!
        _il.Emit(OpCodes.Ldarg_0);

        // now let's pop a value.
        switch (purp)
        {
            case StackValuePurpose.Consume:
            case StackValuePurpose.ToLocal:
            case StackValuePurpose.ReturnToStack:
            default:
                // we need raw value
                _il.Emit(OpCodes.Call, StackPopMethods[primitive.ToType()]);
                break;
            case StackValuePurpose.Target:
                // resolve now
                _il.Emit(OpCodes.Call, StackPopMethods[typeof(Reference)]);
                _il.Emit(OpCodes.Call, ResolveAnyObject);
                break;
            case StackValuePurpose.ArrayTarget:
                // resolve now
                _il.Emit(OpCodes.Call, StackPopMethods[typeof(Reference)]);
//TODO resolve
                break;
            case StackValuePurpose.MethodArg:
            case StackValuePurpose.FieldValue:
                // we need EXACT value
                var real = GetExactType(stackPos);
                var poppable = GetStackTypeFor(real);
                _il.Emit(OpCodes.Call, StackPopMethods[poppable]);
                var marshaller = MarshalUtils.GetMarshallerFor(poppable, false, real, false);
                if (marshaller != null)
                    _il.Emit(OpCodes.Call, marshaller);
                break;
        }
    }

    private Type GetExactType(int stackPos)
    {
        for (int i = _instrIndex + 1; i < _javaBody.AuxiliaryLinkerOutput.Length; i++)
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

    private Type GetArrayType(int stackPos)
    {
        throw new NotImplementedException();
    }
}