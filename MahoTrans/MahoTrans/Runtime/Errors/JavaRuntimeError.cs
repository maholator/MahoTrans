// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime.Errors;

public class JavaRuntimeError : Exception
{
    public JavaRuntimeError()
    {
    }

    public JavaRuntimeError(string? message) : base(message)
    {
    }

    public JavaRuntimeError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}