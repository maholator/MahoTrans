// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using static MahoTrans.Compiler.CompilerUtils;
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

        if (method.ReturnType == typeof(void))
        {
            // stack is here, delete it.
            il.Emit(OpCodes.Pop);
        }
        else if (method.ReturnType.IsAssignableTo(typeof(Object)))
        {
            // we need to take the reference
            il.Emit(OpCodes.Ldfld, typeof(Object).GetField(nameof(Object.HeapAddress))!);
            // now push
            il.Emit(OpCodes.Call, StackPushMethods[typeof(int)]);
        }
        else
        {
            // frame reference is here from the beginning
            il.Emit(OpCodes.Call, StackPushMethods[method.ReturnType]);
        }

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
    ///     This enters with "empty" stack (there are previous args). Take arg0. Call reverse popper. Then apply any conversions needed. This must exit with ready
    ///     value on stack.
    /// </remarks>
    private static void EmitParameterMarshalling(ILGenerator il, ParameterInfo parameter)
    {
        var paramType = parameter.ParameterType;

        // frame object
        il.Emit(OpCodes.Ldarg_0);

        // primitive & ref
        if (StackReversePopMethods.TryGetValue(paramType, out var popper))
        {
            il.Emit(OpCodes.Call, popper);
            return;
        }

        // strings
        if (paramType == typeof(string))
        {
            il.Emit(OpCodes.Call, StackReversePopMethods[typeof(Reference)]);
            il.Emit(OpCodes.Call, IsNullable(parameter) ? ResolveStringOrNullEx : ResolveStringEx);
            return;
        }

        // array
        // check for popper works as check for supported primitive.
        if (paramType.IsArray && StackReversePopMethods.ContainsKey(paramType.GetElementType()!))
        {
            il.Emit(OpCodes.Call, StackReversePopMethods[typeof(Reference)]);
            var resolver = IsNullable(parameter) ? ResolveArrOrNullEx : ResolveArrEx;
            il.Emit(OpCodes.Call, resolver.MakeGenericMethod(paramType.GetElementType()!));
            return;
        }

        // object
        if (paramType.IsAssignableTo(typeof(Object)))
        {
            il.Emit(OpCodes.Call, StackReversePopMethods[typeof(Reference)]);
            var resolver = IsNullable(parameter) ? ResolveObjectOrNullEx : ResolveObjectEx;
            il.Emit(OpCodes.Call, resolver.MakeGenericMethod(paramType));
            return;
        }

        // enum
        if (paramType.IsEnum)
        {
            il.Emit(OpCodes.Call, StackReversePopMethods[Enum.GetUnderlyingType(paramType)]);
            return;
        }

        throw new NotImplementedException($"This parameter ({paramType}) can't be marshalled.");
    }
}