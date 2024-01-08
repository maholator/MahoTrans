// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Utils;

public static class ReferenceExtensions
{
    public static Object AsObject(this Reference r)
    {
        return JvmState.Context.ResolveObject(r);
    }

    public static T As<T>(this Reference r) where T : java.lang.Object
    {
        return JvmState.Context.Resolve<T>(r);
    }
    public static T? AsNullable<T>(this Reference r) where T : java.lang.Object
    {
        return JvmState.Context.ResolveNullable<T>(r);
    }
}