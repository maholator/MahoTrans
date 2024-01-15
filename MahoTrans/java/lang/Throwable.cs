// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace java.lang;

public class Throwable : Object
{
    [String] public Reference Message;
    [JavaIgnore] public Reference[]? StackTrace;

    [InitMethod]
    public new void Init()
    {
        base.Init();
        fillInStackTrace();
        Message = Reference.Null;
    }

    [InitMethod]
    public void Init([String] Reference message)
    {
        base.Init();
        fillInStackTrace();
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
        var name = JavaClass.Name.Replace('/', '.');
        if (Message == Reference.Null)
            return Jvm.AllocateString(name);
        var msg = $"{name}: {Jvm.ResolveStringOrDefault(Message)}";
        return Jvm.AllocateString(msg);
    }

    void fillInStackTrace()
    {
        //TODO java file names & line numbers
        JavaThread? thread = Thread.CurrentThread;
        if (thread == null)
            return;
        StackFrame[] nativeTrace = new StackTrace(true).GetFrames();
        Reference[] tmp = new Reference[thread.ActiveFrameIndex + nativeTrace.Length - 1];
        int j = 0;
        for (int i = 2; i < nativeTrace.Length; i++)
        {
            StringBuilder s = new StringBuilder("<unknown method>");
            MethodBase? method = nativeTrace[i].GetMethod();
            if (method != null)
            {
                s.Length = 0;
                if (method.DeclaringType != null)
                {
                    s.Append(method.DeclaringType == null ? "" : method.DeclaringType.FullName!.Replace('+', '.'));
                    s.Append('.');
                }
                s.Append(method.Name);
                if (s.ToString().Contains("JvmState.Throw") || s.ToString().Contains("Exception.Init")) continue;
                if (s.ToString().Contains("Bridge.bridge_")) break;
                ParameterInfo[]? p = null;
                try
                {
                    p = method.GetParameters();
                }
                catch
                {
                }
                if (p != null)
                {
                    s.Append('(');
                    for (int k = 0; k < p.Length; k++)
                    {
                        if (p[k].ParameterType != null)
                            s.Append(p[k].ParameterType.Name);
                        else
                            s.Append("<unknown type>");
                        //s.Append(' ');
                        //s.Append(p[k].Name);
                        s.Append(',');
                    }
                    s.Length -= 1;
                    s.Append(')');
                }
            }
            if (nativeTrace[i].HasSource())
            {
                string? filename = nativeTrace[i].GetFileName();
                if (filename != null && (filename.Contains('\\') || filename.Contains('/')))
                {
                    int idx = filename.LastIndexOf('\\');
                    filename = filename[((idx == -1 ? filename.LastIndexOf('/') : idx)+1)..];
                }
                s.Append(" (");
                s.Append(filename);
                s.Append(':');
                s.Append(nativeTrace[i].GetFileLineNumber());
                s.Append(')');
            }
            s.Append(" (native)");
            tmp[j++] = Jvm.AllocateString(s.ToString());
        }
        for (int i = thread.ActiveFrameIndex; i >= 0; i--)
        {
            Frame? frame = thread.CallStack[i];
            string s = "<unknown frame>";
            if (frame != null)
            {
                Method? method = frame?.Method.Method;
                if (method == null)
                {
                    s = "<unknown method>";
                }
                else
                {
                    s = $"{method.Class.Name.Replace('/', '.')}.{method.Descriptor}:{frame?.Pointer}";
                    if (method.IsNative)
                        s += " (native)";
                }
            }
            tmp[j++] = Jvm.AllocateString(s);
        }
        StackTrace = new Reference[j];
        global::System.Array.Copy(tmp, StackTrace, j);
        Object.Jvm.Toolkit.Logger?.LogDebug(DebugMessageCategory.Exceptions, $"Constructed {JavaClass.Name}");
    }
}