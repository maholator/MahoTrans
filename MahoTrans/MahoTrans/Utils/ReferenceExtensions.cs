// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public static Object AsObject(this Reference r)
    {
        return JvmContext.Jvm!.ResolveObject(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.Resolve{T}" />.
    /// </summary>
    public static T As<T>(this Reference r) where T : Object
    {
        return JvmContext.Jvm!.Resolve<T>(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.ResolveOrNull{T}" />.
    /// </summary>
    public static T? AsOrNull<T>(this Reference r) where T : Object
    {
        return JvmContext.Jvm!.ResolveOrNull<T>(r);
    }

    public static string AsString(this Reference r) => JvmContext.Jvm!.ResolveString(r);

    public static string? AsStringOrNull(this Reference r) => JvmContext.Jvm!.ResolveStringOrNull(r);

    public static T[] AsArray<T>(this Reference r) where T : struct => JvmContext.Jvm!.ResolveArray<T>(r);

    public static T[]? AsArrayOrNull<T>(this Reference r) where T : struct => JvmContext.Jvm!.ResolveArrayOrNull<T>(r);
}