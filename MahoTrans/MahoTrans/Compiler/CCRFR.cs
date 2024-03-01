// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public bool StackOnEnter;
    public ushort StackOnExit;


    public static implicit operator Range(CCRFR ccrfr)
    {
        var end = ccrfr.Start + ccrfr.Length + 1;
        return new Range(ccrfr.Start, end);
    }
}