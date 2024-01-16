// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
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
        Debug.Assert(t.Source != default, "Attempt to throw throwable without captured context!");
        Throwable = t.This;
    }
}