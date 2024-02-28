// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Compiler;

/// <summary>
///     This tracks purpose of value, pushed to stack by JVM. This is obtained by reversed method pass and can be used to
///     apply conversions/marshallers to value while it's on stack top.
/// </summary>
public enum StackValuePurpose
{
    /// <summary>
    ///     This will be consumed as is by IL opcode.
    /// </summary>
    Consume,

    /// <summary>
    ///     This will be a target of field get/set or native call.
    /// </summary>
    Target,

    /// <summary>
    ///     This will be passed as argument. Marsahlling may need to be applied.
    /// </summary>
    MethodArg,

    /// <summary>
    ///     This must be left in JVM stack after range exit.
    /// </summary>
    ReturnToStack,
}