// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using System.Diagnostics;
using MahoTrans;
using MahoTrans.Runtime.Exceptions;

namespace java.lang;

public class Throwable : Object
{
    [String] public Reference Message;

    /// <summary>
    /// Stack trace. Deeper methods come first.
    /// </summary>
    [JavaIgnore] public IMTStackFrame[]? StackTrace;

    public ThrowSource Source;

    [InitMethod]
    public new void Init()
    {
        base.Init();
        CaptureStackTrace();
        Message = Reference.Null;
    }

    [InitMethod]
    public void Init([String] Reference message)
    {
        base.Init();
        CaptureStackTrace();
        Message = message;
    }

    public void printStackTrace()
    {
        Jvm.Toolkit.System.PrintException(This);
    }

    [return: String]
    public Reference getMessage() => Message;

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(ToString());
    }

    /// <summary>
    /// Captures stack trace. This must be called in initialization method.
    /// </summary>
    [JavaIgnore]
    public void CaptureStackTrace()
    {
        JavaThread? thread = Thread.CurrentThread;
        if (thread == null)
            throw new JavaRuntimeError("Throwables must be constructed inside thread context.");

        StackFrame[] nativeTrace = new StackTrace(true).GetFrames();

        // here we will put all the data
        List<IMTStackFrame> stack = new List<IMTStackFrame>(thread.ActiveFrameIndex + nativeTrace.Length);

        // three frames are always skipped: this method, init() and caller (interpreter or Throw<T>())
        for (int i = 3; i < nativeTrace.Length; i++)
        {
            var ntf = nativeTrace[i];
            var m = ntf.GetMethod();

            // external frame?
            if (m == null)
                continue;

            // we reached interpreter entry point?
            if (m.DeclaringType == typeof(JavaRunner) && m.Name == nameof(JavaRunner.Step))
                break;
            // let's add bridges for now.

            stack.Add(new NativeStackFrame(m, ntf.GetFileName(), ntf.GetFileLineNumber()));
        }

        // we are at interpreter level: let's add java frames.
        for (int i = thread.ActiveFrameIndex; i >= 0; i--)
        {
            Frame frame = thread.CallStack[i]!;
            stack.Add(new JavaStackFrame(frame));
        }

        StackTrace = stack.ToArray();
        Jvm.Toolkit.Logger?.LogRuntime(LogLevel.Info, $"Captured trace for {JavaClass.Name}");
    }

    [JavaIgnore]
    public new string ToString()
    {
        var name = JavaClass.Name.Replace('/', '.');
        if (Message == Reference.Null)
            return name;
        return $"{name}: {Jvm.ResolveStringOrDefault(Message)}";
    }
}