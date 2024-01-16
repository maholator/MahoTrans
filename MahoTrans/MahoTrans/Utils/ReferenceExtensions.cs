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
        return JvmState.Context.ResolveObject(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.Resolve{T}" />.
    /// </summary>
    public static T As<T>(this Reference r) where T : Object
    {
        return JvmState.Context.Resolve<T>(r);
    }

    /// <summary>
    ///     Calls <see cref="JvmState.ResolveNullable{T}" />.
    /// </summary>
    public static T? AsNullable<T>(this Reference r) where T : Object
    {
        return JvmState.Context.ResolveNullable<T>(r);
    }
}