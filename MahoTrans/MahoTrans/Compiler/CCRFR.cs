// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

/// <summary>
///     Cross-Compiled Routine Found Range.
/// </summary>
public struct CCRFR
{
    /// <summary>
    ///     First opcode in range.
    /// </summary>
    public int Start;

    /// <summary>
    ///     Length of range.
    /// </summary>
    public int Length;

    public ushort MaxStackSize;
    public PrimitiveType? StackOnEnter;
    public ushort StackOnExit;

    public int EndExclusive => Start + Length + 1;

    public bool TerminatesMethod(JavaMethodBody jmb) => EndExclusive == jmb.LinkedCode.Length;

    public static implicit operator Range(CCRFR ccrfr)
    {
        return new Range(ccrfr.Start, ccrfr.EndExclusive);
    }
}