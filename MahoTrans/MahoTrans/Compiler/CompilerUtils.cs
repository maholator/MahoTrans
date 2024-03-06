// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Compiler;

/// <summary>
///     Single place for all stuff used by all compilers.
/// </summary>
public static class CompilerUtils
{
    private static NullabilityInfoContext _nullability = new();

    /// <summary>
    ///     Name of a class that will host all bridge methods.
    /// </summary>
    public const string BRIDGE_CLASS_NAME = "MTBridgesHost";

    private static Type Jvm => typeof(JvmState);

    private static Type RefExt => typeof(ReferenceExtensions);

    #region Stack push / pop

    public static readonly Dictionary<Type, MethodInfo> StackPopMethods = new()
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

    public static readonly Dictionary<Type, MethodInfo> StackReversePopMethods = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PopIntFrom))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PopLongFrom))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PopFloatFrom))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PopDoubleFrom))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PopBoolFrom))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PopByteFrom))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PopCharFrom))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PopShortFrom))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PopReferenceFrom))! }
    };

    public static readonly Dictionary<Type, MethodInfo> StackPushMethods = new()
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

    public static readonly MethodInfo StackSetFrom = typeof(Frame).GetMethod(nameof(Frame.SetFrom))!;

    public static readonly MethodInfo StackDiscard = typeof(Frame).GetMethod(nameof(Frame.Discard))!;

    #endregion

    #region Locals

    public static readonly Dictionary<PrimitiveType, MethodInfo> LocalGetMethods = new()
    {
        { PrimitiveType.Int , typeof(Frame).GetMethod(nameof(Frame.GetLocalInt))! },
        { PrimitiveType.Long, typeof(Frame).GetMethod(nameof(Frame.GetLocalLong))! },
        { PrimitiveType.Float, typeof(Frame).GetMethod(nameof(Frame.GetLocalFloat))! },
        { PrimitiveType.Double, typeof(Frame).GetMethod(nameof(Frame.GetLocalDouble))! },
        { PrimitiveType.Reference, typeof(Frame).GetMethod(nameof(Frame.GetLocalReference))! },
    };

    public static readonly Dictionary<PrimitiveType, MethodInfo> LocalSetMethods = new()
    {
        { PrimitiveType.Int , typeof(Frame).GetMethod(nameof(Frame.SetLocalInt))! },
        { PrimitiveType.Long, typeof(Frame).GetMethod(nameof(Frame.SetLocalLong))! },
        { PrimitiveType.Float, typeof(Frame).GetMethod(nameof(Frame.SetLocalFloat))! },
        { PrimitiveType.Double, typeof(Frame).GetMethod(nameof(Frame.SetLocalDouble))! },
        { PrimitiveType.Reference, typeof(Frame).GetMethod(nameof(Frame.SetLocalReference))! },
    };

    #endregion

    /// <summary>
    ///     Static field where context JVM is stored.
    /// </summary>
    public static readonly FieldInfo Context = typeof(JvmContext).GetField(nameof(JvmContext.Jvm))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveObject" />
    /// </summary>
    public static readonly MethodInfo ResolveAnyObject = Jvm.GetMethod(nameof(JvmState.ResolveObject))!;

    /// <summary>
    ///     <see cref="JvmState.Resolve{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveObject = Jvm.GetMethod(nameof(JvmState.Resolve))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveOrNull{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveObjectOrNull = Jvm.GetMethod(nameof(JvmState.ResolveOrNull))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveString" />
    /// </summary>
    public static readonly MethodInfo ResolveString = Jvm.GetMethod(nameof(JvmState.ResolveString))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveStringOrNull" />
    /// </summary>
    public static readonly MethodInfo ResolveStringOrNull = Jvm.GetMethod(nameof(JvmState.ResolveStringOrNull))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveArray{T}" />. Make sure to construct generic method.
    /// </summary>
    public static readonly MethodInfo ResolveArr = Jvm.GetMethod(nameof(JvmState.ResolveArray))!;

    /// <summary>
    ///     <see cref="JvmState.ResolveArrayOrNull{T}" />. Make sure to construct generic method.
    /// </summary>
    public static readonly MethodInfo ResolveArrOrNull = Jvm.GetMethod(nameof(JvmState.ResolveArrayOrNull))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsObject" />
    /// </summary>
    public static readonly MethodInfo ResolveAnyObjectEx = RefExt.GetMethod(nameof(ReferenceExtensions.AsObject))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.As{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveObjectEx = RefExt.GetMethod(nameof(ReferenceExtensions.As))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsOrNull{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveObjectOrNullEx = RefExt.GetMethod(nameof(ReferenceExtensions.AsOrNull))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsString" />
    /// </summary>
    public static readonly MethodInfo ResolveStringEx = RefExt.GetMethod(nameof(ReferenceExtensions.AsString))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsStringOrNull" />
    /// </summary>
    public static readonly MethodInfo ResolveStringOrNullEx =
        RefExt.GetMethod(nameof(ReferenceExtensions.AsStringOrNull))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsArray{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveArrEx = RefExt.GetMethod(nameof(ReferenceExtensions.AsArray))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.AsArrayOrNull{T}" />
    /// </summary>
    public static readonly MethodInfo ResolveArrOrNullEx = RefExt.GetMethod(nameof(ReferenceExtensions.AsArrayOrNull))!;

    /// <summary>
    ///     <see cref="ReferenceExtensions.GetAddrSafely" />
    /// </summary>
    public static readonly MethodInfo GetAddressSafely = RefExt.GetMethod(nameof(ReferenceExtensions.GetAddrSafely))!;

    public static bool IsNullable(ParameterInfo param)
    {
        return _nullability.Create(param).WriteState == NullabilityState.Nullable;
    }

    /// <summary>
    ///     Gets type that needs to be taken from stack if you need to pass it as a parameter. You may need to apply marshaller
    ///     to it. This also may be used in opposite situation.
    /// </summary>
    /// <param name="t">Parameter to get type for.</param>
    /// <returns>Type to pop/push from stack. This is guaranteed to be supported by <see cref="StackReversePopMethods" />.</returns>
    public static Type GetStackTypeFor(Type t)
    {
        // primitive & ref
        if (StackReversePopMethods.ContainsKey(t))
            return t;

        // strings
        if (t == typeof(string))
            return typeof(Reference);

        // array
        if (t.IsArray && StackReversePopMethods.ContainsKey(t.GetElementType()!))
            return typeof(Reference);

        // object
        if (t.IsAssignableTo(typeof(Object)))
            return typeof(Reference);

        // enum
        if (t.IsEnum)
            return Enum.GetUnderlyingType(t);

        throw new NotImplementedException($"This parameter ({t}) can't be popped from stack directly.");
    }
}