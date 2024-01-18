// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using java.lang;

namespace MahoTrans;

public class JavaUnhandledException : JavaRuntimeError
{
    public JavaUnhandledException(string message, Throwable obj) : base(message)
    {
        Debug.Assert(obj != null);
        Throwable = obj;
    }

    public readonly Throwable Throwable;
}