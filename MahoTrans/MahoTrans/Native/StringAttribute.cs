// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

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
