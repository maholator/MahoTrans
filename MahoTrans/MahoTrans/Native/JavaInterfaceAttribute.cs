// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Native;

/// <summary>
///     Mark an interface with this to make it visible in JVM.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
[MeansImplicitUse]
public class JavaInterfaceAttribute : Attribute
{
    public Type? ReferenceImplementation { get; }

    public JavaInterfaceAttribute()
    {
    }

    public JavaInterfaceAttribute(Type? referenceImplementation)
    {
        ReferenceImplementation = referenceImplementation;
    }
}