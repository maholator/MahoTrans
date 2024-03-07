// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime.Errors;

/// <summary>
///     Thrown by <see cref="BytecodeLinker" /> if it detected invalid jump in a method.
/// </summary>
public class BrokenFlowException : Exception
{
    public BrokenFlowException(string? message)
        : base(message)
    {
    }
}
