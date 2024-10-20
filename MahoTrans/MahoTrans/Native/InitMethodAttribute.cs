// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
///     Mark a method with this to make it class instance initializer. Method's name will be discarded. Method must return
///     void.
/// </summary>
/// <seealso cref="ClassInitAttribute" />
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class InitMethodAttribute : Attribute
{
}
