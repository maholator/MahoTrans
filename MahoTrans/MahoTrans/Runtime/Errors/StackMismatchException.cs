// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace MahoTrans.Runtime.Errors;

/// <summary>
///     Thrown by <see cref="BytecodeLinker" /> if it detected a mismatch in the stack.
/// </summary>
public class StackMismatchException : Exception
{
    public StackMismatchException(string? message)
        : base(message)
    {
    }
}
