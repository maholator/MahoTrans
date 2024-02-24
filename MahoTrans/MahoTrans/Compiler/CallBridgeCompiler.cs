// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using Object = java.lang.Object;

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
            il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.SetFrom))!);


            if (!method.IsStatic)
            {
                // frame
                il.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // frame > heap
                il.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                il.Emit(OpCodes.Call, CompilerUtils.StackReversePopMethods[typeof(Reference)]);
                // frame > heap > ref
                il.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // frame > object
            }

            foreach (var parameter in method.GetParameters())
            {
                il.Emit(OpCodes.Ldarg_0);
                var popper = CompilerUtils.StackReversePopMethods[parameter.ParameterType];
                il.Emit(OpCodes.Call, popper);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, argsLength);
            il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.Discard))!);
        }

        il.Emit(OpCodes.Call, method);

        if (method.ReturnType != typeof(void))
        {
            // frame reference is here from the beginning
            il.Emit(OpCodes.Call, CompilerUtils.StackPushMethods[method.ReturnType]);
        }
        else
        {
            il.Emit(OpCodes.Pop);
        }

        il.Emit(OpCodes.Ret);

        return num;
    }
}