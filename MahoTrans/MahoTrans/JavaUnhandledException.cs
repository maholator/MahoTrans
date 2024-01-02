// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans;

public class JavaUnhandledException : JavaRuntimeError
{
    public JavaUnhandledException(string message, JavaThrowable innerException) : base(message, innerException)
    {
    }
}