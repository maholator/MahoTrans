// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Compiler;

public enum OpcodeType
{
    /// <summary>
    ///     This opcode doesn't do anything.
    /// </summary>
    NoOp = 1,

    /// <summary>
    ///     This opcode loads a constant.
    /// </summary>
    Constant,

    /// <summary>
    ///     This opcode modifies or reads a local variable.
    /// </summary>
    Local,

    /// <summary>
    ///     This opcode reads or modifies an array element.
    /// </summary>
    Array,

    /// <summary>
    ///     This opcode does a no-op stack manipulation, i.e. pop/dup/swap.
    /// </summary>
    Stack,

    /// <summary>
    ///     This opcode does math operation.
    /// </summary>
    Math,

    /// <summary>
    ///     This opcode converts value from one type to another.
    /// </summary>
    Conversion,

    /// <summary>
    ///     This opcode compares two values.
    /// </summary>
    Compare,

    /// <summary>
    ///     This opcode does branching.
    /// </summary>
    Branch,

    /// <summary>
    ///     This opcode does jump.
    /// </summary>
    Jump,

    /// <summary>
    ///     This opcode terminates a method.
    /// </summary>
    Return,

    /// <summary>
    ///     This opcode throws an exception.
    /// </summary>
    Throw,

    /// <summary>
    ///     This calls prelinked method.
    /// </summary>
    Call,

    /// <summary>
    ///     This calls virtual method.
    /// </summary>
    VirtCall,

    /// <summary>
    ///     This reads/writes static.
    /// </summary>
    Static,

    /// <summary>
    ///     This opcode allocates something.
    /// </summary>
    Alloc,

    Monitor,

    Cast,

    Bridge,

    /// <summary>
    ///     This opcode has attached class initializer.
    /// </summary>
    Initializer,

    Error,
}