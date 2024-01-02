// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans;

public class JavaLinkageException : Exception
{
    public JavaLinkageException(string? message) : base(message)
    {
    }

    public JavaLinkageException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}