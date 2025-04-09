// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
///     Mark a method with this to make it class initializer (static block). Method's name will be discarded. Method must
///     return void.
/// </summary>
/// <seealso cref="InitMethodAttribute" />
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class ClassInitAttribute : Attribute
{
}
