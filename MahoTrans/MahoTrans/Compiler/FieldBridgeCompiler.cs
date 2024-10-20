// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Compiler;

/// <summary>
///     Can build IL bridge that moves data between CLR field and JVM stack.
/// </summary>
public static class FieldBridgeCompiler
{
    public static void BuildBridges(TypeBuilder typeBuilder, FieldInfo field, NameDescriptor name, JavaClass cls)
    {
        {
            var getter = DefineBridge(GetGetterName(name, cls.Name), typeBuilder);

            getter.Emit(OpCodes.Ldarg_0);
            // frame

            if (field.IsStatic)
            {
                getter.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                getter.Emit(OpCodes.Ldsfld, CompilerUtils.Context);
                // frame > heap
                getter.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                getter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[typeof(Reference)]);
                // frame > heap > ref
                getter.Emit(OpCodes.Call, CompilerUtils.ResolveAnyObject);
                // frame > object
                getter.Emit(OpCodes.Ldfld, field);
            }

            // frame > value
            getter.Emit(OpCodes.Call, CompilerUtils.StackPushMethods[field.FieldType]);
            // -
            getter.Emit(OpCodes.Ret);
        }
        {
            var setter = DefineBridge(GetSetterName(name, cls.Name), typeBuilder);

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
                setter.Emit(OpCodes.Ldsfld, CompilerUtils.Context);
                // heap
                setter.Emit(OpCodes.Ldarg_0);
                // heap > frame
                setter.Emit(OpCodes.Call, CompilerUtils.StackPopMethods[typeof(Reference)]);
                // heap > ref
                setter.Emit(OpCodes.Call, CompilerUtils.ResolveAnyObject);
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
    ///     Builds GET bridge for field, placed in <see cref="StaticMemory" />.
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

        var getter = DefineBridge(GetGetterName(attr.AsDescriptor(), attr.OwnerName), typeBuilder);

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
    ///     Gets a good name for a field in CLR type built from JVM one.
    /// </summary>
    /// <param name="descriptor">Field descriptor.</param>
    /// <param name="className">Class, where the field is declared.</param>
    /// <returns>Name for field.</returns>
    public static string GetFieldName(NameDescriptor descriptor, string className)
    {
        return $"{className.Length}_{descriptor.GetSnapshotHash()}_{className}_{descriptor}";
    }

    public static string GetGetterName(NameDescriptor descriptor, string className) =>
        GetFieldName(descriptor, className) + "_bridge_get";

    public static string GetSetterName(NameDescriptor descriptor, string className) =>
        GetFieldName(descriptor, className) + "_bridge_set";

    public static Action<Frame>? CaptureGetter(Type bridgeHost, JavaClass cls, Field field)
    {
        var method = bridgeHost.GetMethod(GetGetterName(field.Descriptor, cls.Name));
        return method?.CreateDelegate<Action<Frame>>();
    }

    public static Action<Frame>? CaptureSetter(Type bridgeHost, JavaClass cls, Field field)
    {
        var method = bridgeHost.GetMethod(GetSetterName(field.Descriptor, cls.Name));
        return method?.CreateDelegate<Action<Frame>>();
    }
}
