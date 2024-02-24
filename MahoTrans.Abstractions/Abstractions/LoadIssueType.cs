// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Abstractions;

/// <summary>
///     Various issues with bytecode that linker can detect at load-time.
/// </summary>
public enum LoadIssueType : byte
{
    /// <summary>
    ///     Method doesn't return or contains invalid jumps.
    /// </summary>
    [Description("Method doesn't return")] BrokenFlow = 1,

    /// <summary>
    ///     xload/xstore opcode is used on an invalid local variable.
    /// </summary>
    [Description("Invalid local")] LocalVariableIndexOutOfBounds,

    /// <summary>
    ///     Emulated stack detected type mismatch.
    /// </summary>
    [Description("Stack mismatch")] StackMismatch,

    /// <summary>
    ///     There is an access to class field or method, this class is going to be casted into or instantiated, but this class
    ///     is not available.
    /// </summary>
    [Description("Missing class access")] MissingClassAccess,

    /// <summary>
    ///     There is an access to existing class' method, but this method is not available.
    /// </summary>
    [Description("Missing method access")] MissingMethodAccess,

    /// <summary>
    ///     There is an access to existing class' method via virtual call, but this method is not available.
    /// </summary>
    [Description("Missing virtual access")] MissingVirtualAccess,

    /// <summary>
    ///     There is an access to existing class' field, but that field is not available.
    /// </summary>
    [Description("Missing field access")] MissingFieldAccess,

    /// <summary>
    ///     There is an existing field in existing class, but type of this field is not available.
    /// </summary>
    [Description("Field of missing class")]
    MissingClassField,

    /// <summary>
    ///     There is a class, but its super class is not available.
    /// </summary>
    [Description("Missing class extension")]
    MissingClassSuper,

    /// <summary>
    ///     Constant has unexpected type or index.
    /// </summary>
    [Description("Invalid constant")] InvalidConstant,

    /// <summary>
    ///     Jar package has no manifest.
    /// </summary>
    [Description("Missing MANIFEST.MF")] NoMetaInf,

    /// <summary>
    ///     Class file has invalid magic code.
    /// </summary>
    [Description("Invalid magic")] InvalidClassMagicCode,

    /// <summary>
    ///     There is a local variable that has multiple types.
    /// </summary>
    [Description("Multitype local")] MultiTypeLocalVariable,

    /// <summary>
    ///     Something in native code looks wrong, but we can live with it.
    /// </summary>
    [Description("Questionable native code")]
    QuestionableNativeCode,
}