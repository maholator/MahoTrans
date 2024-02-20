// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime.Errors;

/// <summary>
///     Thrown by <see cref="BytecodeLinker" /> if it detected a mismatch in the stack.
/// </summary>
public class StackMismatchException : Exception
{
    public StackMismatchException(string? message) : base(message)
    {
    }
}