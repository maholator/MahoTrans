// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
///     Mark a parameter or return value of method to tell the compiler which JVM type is it. If not present on reference
///     parameter, java/lang/Object will be used.
/// </summary>
/// <seealso cref="JavaDescriptorAttribute" />
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field)]
[MeansImplicitUse]
public class JavaTypeAttribute : Attribute
{
    public string Name;

    /// <summary>
    ///     Sets java type name. L{}; are automatically appended. If array type is passed, it left as is.
    /// </summary>
    /// <param name="name">Name of the type.</param>
    /// <remarks>
    ///     Do not use nameof(), as it passes non-full name! Use typeof() to get full names.
    /// </remarks>
    public JavaTypeAttribute(string name)
    {
        // arrays are always okay. Too short types are probably okay too (unit tests, global ad-hocs, etc.)
        // Otherwise, type must be in a non-global package.
        if (name[0] != '[' && name.Length > 6 && name.IndexOf('/') == -1)
            throw new ArgumentException(
                $"Suspicious type name - you used nameof() instead of typeof()? Type name: {name}");

        Name = name;
    }

    public JavaTypeAttribute(Type type)
    {
        Name = type.FullName!.Replace('.', '/');
    }
}