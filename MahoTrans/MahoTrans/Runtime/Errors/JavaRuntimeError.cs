// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace MahoTrans.Runtime.Errors;

public class JavaRuntimeError : Exception
{
    public JavaRuntimeError()
    {
    }

    public JavaRuntimeError(string? message)
        : base(message)
    {
    }

    public JavaRuntimeError(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
