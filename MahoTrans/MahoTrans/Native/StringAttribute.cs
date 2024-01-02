// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
///     Marks reference that it contains java string object.
/// </summary>
/// <seealso cref="JavaTypeAttribute" />
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field)]
[MeansImplicitUse]
public class StringAttribute : Attribute
{
}