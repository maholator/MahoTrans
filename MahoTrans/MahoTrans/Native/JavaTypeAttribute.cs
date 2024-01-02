// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public JavaTypeAttribute(string name)
    {
        Name = name;
    }

    public JavaTypeAttribute(Type type)
    {
        Name = type.FullName!.Replace('.', '/');
    }
}