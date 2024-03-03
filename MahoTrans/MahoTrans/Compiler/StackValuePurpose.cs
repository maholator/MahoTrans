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
    ///     This will be consumed as is by opcode.
    /// </summary>
    Consume = 1,

    /// <summary>
    ///     This will be a target of field get/set or call.
    /// </summary>
    Target,

    ArrayTargetByte,
    ArrayTargetChar,
    ArrayTargetShort,
    ArrayTargetInt,
    ArrayTargetLong,
    ArrayTargetFloat,
    ArrayTargetDouble,
    ArrayTargetRef,

    /// <summary>
    ///     This will be passed as argument. Marsahlling may need to be applied.
    /// </summary>
    MethodArg,

    /// <summary>
    ///     This will be pushed to field.
    /// </summary>
    FieldValue,

    /// <summary>
    ///     This will be pushed to local.
    /// </summary>
    ToLocal,

    /// <summary>
    ///     This will be returned to stack. This must be used only inside compiler.
    /// </summary>
    ReturnToStack,
}