// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

/// <summary>
///     Single place for all stuff used by all compilers.
/// </summary>
public static class CompilerUtils
{
    /// <summary>
    ///     Name of a class that will host all bridge methods.
    /// </summary>
    public const string BRIDGE_CLASS_NAME = "MTBridgesHost";

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

    /// <summary>
    /// Static field where context JVM is stored.
    /// </summary>
    public static readonly FieldInfo Context = typeof(JvmContext).GetField(nameof(JvmContext.Jvm))!;

    /// <summary>
    /// <see cref="JvmState.ResolveObject"/>
    /// </summary>
    public static readonly MethodInfo ResolveAnyObject = typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!;
}