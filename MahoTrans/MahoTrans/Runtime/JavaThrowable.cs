// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using java.lang;
using Exception = System.Exception;

namespace MahoTrans.Runtime;

/// <summary>
///     Exception which is thrown in CLR to pass exception to JVM environment.
/// </summary>
public class JavaThrowable : Exception
{
    public Reference Throwable;

    public JavaThrowable(Throwable t)
    {
        Debug.Assert(t.Source != default, "Attempt to throw throwable without known source!");
        Debug.Assert(t.StackTrace != null, "Attempt to throw throwable without captured stack trace!");
        Throwable = t.This;
    }
}
