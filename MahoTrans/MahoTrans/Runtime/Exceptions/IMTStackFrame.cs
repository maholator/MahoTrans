// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime.Exceptions;

/// <summary>
///     <see cref="StackFrame" /> but suitable for mixed code. Call <see cref="object.ToString()"/> to get a quick description.
/// </summary>
public interface IMTStackFrame
{
    /// <summary>
    ///     Name of the method.
    /// </summary>
    string MethodName { get; }

    /// <summary>
    ///     Method signature. For native method, this is a human-readable string of args and return type. For java methods,
    ///     this must return <see cref="Method" />.<see cref="Method.Descriptor" />.<see cref="NameDescriptor.Descriptor" />
    /// </summary>
    string MethodSignature { get; }

    /// <summary>
    ///     Name of the class, where this method is defined. May be null if method is a native global function.
    /// </summary>
    string? MethodClass { get; }

    /// <summary>
    ///     <see cref="JavaClass" /> where the method is defined. Null for native methods.
    /// </summary>
    JavaClass? MethodJavaClass { get; }

    /// <summary>
    ///     <see cref="Type" /> where the method is defined. For native methods, this is the actual type. For java methods,
    ///     this must return CLR part of the class (where fields are defined). May be null if method is a native global
    ///     function.
    /// </summary>
    Type? MethodNativeClass { get; }

    /// <summary>
    ///     Method body of the method. Null if the method is native.
    /// </summary>
    JavaMethodBody? JavaMethod { get; }

    /// <summary>
    ///     Number of java opcode. Null for native methods.
    /// </summary>
    int? OpcodeNumber { get; }

    /// <summary>
    ///     Source code line number. May be not available, null in such case.
    /// </summary>
    int? LineNumber { get; }

    string? SourceFile { get; }
}