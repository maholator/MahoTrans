// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans;

public class StackMismatchException : Exception
{
    public StackMismatchException(string? message) : base(message)
    {
    }
}