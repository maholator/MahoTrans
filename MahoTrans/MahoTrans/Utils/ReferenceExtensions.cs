// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace MahoTrans.Utils;

/// <summary>
///     Compact ways to call resolution methods.
/// </summary>
public static class ReferenceExtensions
{
    /// <summary>
    ///     Calls <see cref="JvmState.ResolveObject" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Object AsObject(this Reference r)
    {
        return JvmContext.Jvm!.ResolveObject(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.Resolve{T}" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T As<T>(this Reference r) where T : class, IJavaObject
    {
        return JvmContext.Jvm!.Resolve<T>(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.ResolveOrNull{T}" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T? AsOrNull<T>(this Reference r) where T : class, IJavaObject
    {
        return JvmContext.Jvm!.ResolveOrNull<T>(r);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string AsString(this Reference r) => JvmContext.Jvm!.ResolveString(r);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string? AsStringOrNull(this Reference r) => JvmContext.Jvm!.ResolveStringOrNull(r);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T[] AsArray<T>(this Reference r) where T : struct => JvmContext.Jvm!.ResolveArray<T>(r);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T[]? AsArrayOrNull<T>(this Reference r) where T : struct => JvmContext.Jvm!.ResolveArrayOrNull<T>(r);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Reference GetAddrSafely(this Object? obj)
    {
        if (obj is null)
            return Reference.Null;
        return obj.This;
    }

    public static void Throw(this Reference exception) => JvmContext.Jvm!.Throw(exception);
}
