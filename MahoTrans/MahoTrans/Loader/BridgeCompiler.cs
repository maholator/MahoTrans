using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

public static class BridgeCompiler
{
    private static readonly Dictionary<Type, MethodInfo> StackPoppers = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PopInt))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PopLong))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PopFloat))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PopDouble))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PopBool))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PopByte))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PopChar))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PopShort))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PopReference))! }
    };

    public static readonly Dictionary<Type, MethodInfo> StackPushers = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PushInt))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PushLong))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PushFloat))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PushDouble))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PushBool))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PushByte))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PushChar))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PushShort))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PushReference))! },
    };

    public static void BuildBridges(TypeBuilder typeBuilder, FieldInfo field, NameDescriptor name, JavaClass cls)
    {
        {
            var getter = DefineBridge(GetFieldGetterName(name, cls), typeBuilder);

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
                getter.Emit(OpCodes.Call, StackPoppers[typeof(Reference)]);
                // frame > heap > ref
                getter.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // frame > object
                getter.Emit(OpCodes.Ldfld, field);
            }

            // frame > value
            getter.Emit(OpCodes.Call, StackPushers[field.FieldType]);
            // -
            getter.Emit(OpCodes.Ret);
        }
        {
            var setter = DefineBridge(GetFieldSetterName(name, cls), typeBuilder);

            if (field.IsStatic)
            {
                setter.Emit(OpCodes.Ldarg_0);
                // frame
                setter.Emit(OpCodes.Call, StackPoppers[field.FieldType]);
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
                setter.Emit(OpCodes.Call, StackPoppers[field.FieldType]);
                // value
                setter.Emit(OpCodes.Stloc, val);
                // -
                setter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // heap
                setter.Emit(OpCodes.Ldarg_0);
                // heap > frame
                setter.Emit(OpCodes.Call, StackPoppers[typeof(Reference)]);
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

    private static ILGenerator DefineBridge(string name, TypeBuilder builder)
    {
        var method = builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, null, new[] { typeof(Frame) });
        method.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        return method.GetILGenerator();
    }

    public static string GetFieldName(NameDescriptor descriptor, JavaClass cls) => $"{cls.Name}_{descriptor}";

    public static string GetFieldGetterName(NameDescriptor descriptor, JavaClass cls) => GetFieldName(descriptor, cls) + "_bridge_get";

    public static string GetFieldSetterName(NameDescriptor descriptor, JavaClass cls) => GetFieldName(descriptor, cls) + "_bridge_set";
}