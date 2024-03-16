// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using static MahoTrans.Compiler.CompilerUtils;

namespace MahoTrans.Compiler;

/// <summary>
///     Marshaller is helper method that you can call to convert value from one type to another.
/// </summary>
/// <remarks>
///     This does not handle value conversion, i.e. int to long. This is done by java opcodes. This handles only marshal
///     conversions.
/// </remarks>
public static class MarshalUtils
{
    public static MethodInfo? GetMarshallerFor(Type from, bool fromNullable, Type to, bool toNullable)
    {
        if (from == to)
            return null;

        if (from.IsEnum && to.IsEnum)
        {
            if (from.GetEnumUnderlyingType() == to.GetEnumUnderlyingType())
                return null;

            throw new NotSupportedException($"Can't marshal {from} to {to} because they have different base types.");
        }

        if (from.IsEnum)
        {
            var underlyingType = from.GetEnumUnderlyingType();
            if (underlyingType == to)
                return null;
            throw new NotSupportedException($"Can't marshal {underlyingType}-based enum to {to}");
        }

        if (to.IsEnum)
        {
            var underlyingType = to.GetEnumUnderlyingType();
            if (underlyingType == from)
                return null;
            throw new NotSupportedException($"Can't marshal {from} to {underlyingType}-based enum");
        }

        if (from == typeof(Reference))
            return GetMarshallerFromRef(to, toNullable);

        if (to == typeof(Reference))
            return GetMarshallerToRef(from);

        throw new NotSupportedException($"{from} can't be marshaled to {to}.");
    }

    private static MethodInfo GetMarshallerFromRef(Type to, bool toNullable)
    {
        if (to.IsJavaType())
        {
            var resolver = toNullable ? ResolveObjectOrNullEx : ResolveObjectEx;
            return resolver.MakeGenericMethod(to);
        }

        if (to == typeof(string))
            return toNullable ? ResolveStringOrNullEx : ResolveStringEx;

        if (to.IsArray && StackReversePopMethods.ContainsKey(to.GetElementType()!))
        {
            var resolver = toNullable ? ResolveArrOrNullEx : ResolveArrEx;
            return resolver.MakeGenericMethod(to.GetElementType()!);
        }

        throw new NotSupportedException($"Can't marshal Reference to {to}.");
    }

    private static MethodInfo GetMarshallerToRef(Type from)
    {
        if (from.IsJavaType())
            return GetAddressSafely;

        if (from == typeof(Reference[]))
            throw new NotSupportedException($"Can't implicitly marshal Reference[] to Reference.");

        if (from.IsArray && StackReversePopMethods.ContainsKey(from.GetElementType()!))
            return WrapArray;

        throw new NotSupportedException($"Can't marshal {from} to Reference.");
    }
}
