// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Exceptions;
using Newtonsoft.Json;

namespace java.lang;

public class Throwable : Object
{
    [String] public Reference Message;

    /// <summary>
    ///     <see cref="Message" />, resolved during constructor execution.
    /// </summary>
    /// <remarks>
    ///     Well, this not how this is supposed to work. But we have what we have.
    ///     Throwable objects are supposed to be stored as is by frontend for various purposes - logging, debugging, etc. While
    ///     it's stored, it will likely be deleted from the heap by GC.
    ///     Attempt to prevent this breaks concept of MT determinism. So, string object that contains message will be lost.
    ///     Usually i would say something like "frontend should capture a full snapshot and work with it" but exception message
    ///     is a TOO IMPORTANT thing because, well, it explains what happened.
    ///     Why <see cref="Message" /> exists? Oh, well, because "new Throwable(abc).getMessage() == abc" must be true because
    ///     java.
    /// </remarks>
    public string? MessageClr;

    /// <summary>
    ///     Stack trace. Deeper methods come first. This must be initialized to something sane during java constructor call.
    /// </summary>
    /// <remarks>
    ///     <see cref="JsonIgnoreAttribute" /> is needed because call stack is stored as references to actual methods. There is
    ///     no way to snapshot call stack.
    /// </remarks>
    [JsonIgnore] [JavaIgnore] public IMTStackFrame[] StackTrace = null!;

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
        MessageClr = Jvm.ResolveStringOrDefault(message);
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
    ///     Captures stack trace. This must be called in initialization method.
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
            // we also want to see interpreter frame, as it contains line number which may be important.

            stack.Add(new NativeStackFrame(m, ntf.GetFileName(), ntf.GetFileLineNumber()));
        }

        // we are at interpreter level: let's add java frames.
        for (int i = thread.ActiveFrameIndex; i >= 0; i--)
        {
            Frame frame = thread.CallStack[i]!;
            stack.Add(new JavaStackFrame(frame));
        }

        StackTrace = stack.ToArray();
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