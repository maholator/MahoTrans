// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Compiler;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

/// <summary>
///     Set of tools to build IL bridges for JVM communication with CLR fields and methods.
/// </summary>
public static class BridgeCompiler
{
    #region Fields

    public static void BuildBridges(TypeBuilder typeBuilder, FieldInfo field, NameDescriptor name, JavaClass cls)
    {
        {
            var getter = DefineBridge(GetFieldGetterName(name, cls.Name), typeBuilder);

            getter.Emit(OpCodes.Ldarg_0);
            // frame

            if (field.IsStatic)
            {
                getter.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                getter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // frame > heap
                getter.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                getter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[typeof(Reference)]);
                // frame > heap > ref
                getter.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // frame > object
                getter.Emit(OpCodes.Ldfld, field);
            }

            // frame > value
            getter.Emit(OpCodes.Call, CompilerUtils.StackPushMethods[field.FieldType]);
            // -
            getter.Emit(OpCodes.Ret);
        }
        {
            var setter = DefineBridge(GetFieldSetterName(name, cls.Name), typeBuilder);

            if (field.IsStatic)
            {
                setter.Emit(OpCodes.Ldarg_0);
                // frame
                setter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[field.FieldType]);
                // value
                setter.Emit(OpCodes.Stsfld, field);
                // -
                setter.Emit(OpCodes.Ret);
            }
            else
            {
                var val = setter.DeclareLocal(field.FieldType);
                setter.Emit(OpCodes.Ldarg_0);
                // frame
                setter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[field.FieldType]);
                // value
                setter.Emit(OpCodes.Stloc, val);
                // -
                setter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // heap
                setter.Emit(OpCodes.Ldarg_0);
                // heap > frame
                setter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[typeof(Reference)]);
                // heap > ref
                setter.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // target
                setter.Emit(OpCodes.Ldloc, val);
                // target > value
                setter.Emit(OpCodes.Stfld, field);
                // -
                setter.Emit(OpCodes.Ret);
            }
        }
    }

    /// <summary>
    /// Builds GET bridge for field, placed in <see cref="StaticMemory"/>.
    /// </summary>
    /// <param name="typeBuilder">Builder to place bridge at.</param>
    /// <param name="field">Field to build bridge for.</param>
    /// <returns>Real field descriptor.</returns>
    public static NameDescriptor BuildNativeStaticBridge(TypeBuilder typeBuilder, FieldInfo field)
    {
        Debug.Assert(field.DeclaringType == typeof(StaticMemory),
            $"Native static field {field.Name} is declared in {field.DeclaringType}, must be in {typeof(StaticMemory)}");

        var attr = field.GetCustomAttribute<NativeStaticAttribute>();
        if (attr == null)
            throw new JavaLinkageException(
                $"{field.Name} does not have {nameof(NativeStaticAttribute)} attached and can't be accessed via bridge.");

        var getter = DefineBridge(GetFieldGetterName(attr.AsDescriptor(), attr.OwnerName), typeBuilder);

        getter.Emit(OpCodes.Ldarg_0);
        // frame
        getter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.NativeStatics))!.GetMethod!);
        // frame > statics
        getter.Emit(OpCodes.Ldfld, field);
        // frame > value
        getter.Emit(OpCodes.Call, CompilerUtils.StackPushMethods[field.FieldType]);
        // -
        getter.Emit(OpCodes.Ret);

        return attr.AsDescriptor();
    }

    private static ILGenerator DefineBridge(string name, TypeBuilder builder)
    {
        var method = builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, null, new[] { typeof(Frame) });
        method.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        return method.GetILGenerator();
    }

    /// <summary>
    /// Gets a good name for a field in CLR type built from JVM one.
    /// </summary>
    /// <param name="descriptor">Field descriptor.</param>
    /// <param name="className">Class, where the field is declared.</param>
    /// <returns>Name for field.</returns>
    public static string GetFieldName(NameDescriptor descriptor, string className)
    {
        return $"{className.Length}_{descriptor.GetSnapshotHash()}_{className}_{descriptor}";
    }

    public static string GetFieldGetterName(NameDescriptor descriptor, string className) =>
        GetFieldName(descriptor, className) + "_bridge_get";

    public static string GetFieldSetterName(NameDescriptor descriptor, string className) =>
        GetFieldName(descriptor, className) + "_bridge_set";

    #endregion

    #region Methods

    private static int _bridgeCounter = 1;

    public static int BuildBridgeMethod(MethodInfo method, TypeBuilder bridgeContainer)
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

    #endregion
}