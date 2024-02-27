// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using static MahoTrans.Compiler.CompilerUtils;

namespace MahoTrans.Compiler;

/// <summary>
///     Can build IL bridge that takes values from JVM stack, converts it, passes to CLR method and pushes result back to
///     JVM stack.
/// </summary>
public static class CallBridgeCompiler
{
    private static int _bridgeCounter = 1;

    public static int BuildCallBridge(MethodInfo method, TypeBuilder bridgeContainer)
    {
        int num = _bridgeCounter;
        _bridgeCounter++;
        var builder = bridgeContainer.DefineMethod($"bridge_{num}", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, null, new[] { typeof(Frame) });
        builder.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        var il = builder.GetILGenerator();
        var argsLength = method.GetParameters().Length;
        if (!method.IsStatic)
            argsLength++;

        // for push operation
        il.Emit(OpCodes.Ldarg_0);

        // all this is skipped if call is static and no args are needed.
        if (argsLength != 0)
        {
            // setting stack pointer for pops
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, argsLength);
            il.Emit(OpCodes.Call, StackSetFrom);


            if (!method.IsStatic)
            {
                // frame
                il.Emit(OpCodes.Ldsfld, Context);
                // frame > heap
                il.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                il.Emit(OpCodes.Call, StackReversePopMethods[typeof(Reference)]);
                // frame > heap > ref
                il.Emit(OpCodes.Call, ResolveAnyObject);
                // frame > object
            }

            foreach (var parameter in method.GetParameters())
            {
                EmitParameterMarshalling(il, parameter);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, argsLength);
            il.Emit(OpCodes.Call, StackDiscard);
        }

        il.Emit(OpCodes.Call, method);

        EmitReturnerMarshalling(il, method);

        il.Emit(OpCodes.Ret);

        return num;
    }

    /// <summary>
    ///     Emits code to convert java primitive to something suitable for passing to the method. This must be synchronized
    ///     with parameter conversion in <see cref="NativeLinker" />. See docs for details.
    /// </summary>
    /// <param name="il">Generator.</param>
    /// <param name="parameter">Parameter to convert.</param>
    /// <remarks>
    ///     Notes for implementation of this: this enters with "empty" stack (there are previous args). Take arg0. Call reverse
    ///     popper. Then apply any conversions needed. This must exit with ready value on stack.
    /// </remarks>
    private static void EmitParameterMarshalling(ILGenerator il, ParameterInfo parameter)
    {
        var paramType = parameter.ParameterType;

        il.Emit(OpCodes.Ldarg_0);
        var poppedType = GetStackTypeFor(parameter);
        il.Emit(OpCodes.Call, StackReversePopMethods[poppedType]);

        var marshaller = MarshalUtils.GetMarshallerFor(poppedType, false, paramType, IsNullable(parameter));
        if (marshaller != null)
            il.Emit(OpCodes.Call, marshaller);
    }

    private static void EmitReturnerMarshalling(ILGenerator il, MethodInfo method)
    {
        var retPar = method.ReturnParameter;
        var retType = retPar.ParameterType;

        if (retType == typeof(void))
        {
            // stack is here, delete it.
            il.Emit(OpCodes.Pop);
        }
        else
        {
            var stType = GetStackTypeFor(retPar);
            // apply marshaller if needed
            var marshaller = MarshalUtils.GetMarshallerFor(retPar.ParameterType, IsNullable(retPar), stType, false);
            if (marshaller != null)
                il.Emit(OpCodes.Call, marshaller);
            // now push
            il.Emit(OpCodes.Call, StackPushMethods[stType]);
        }
    }
}