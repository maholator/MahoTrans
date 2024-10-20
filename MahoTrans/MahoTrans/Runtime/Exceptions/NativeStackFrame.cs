// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Text;
using MahoTrans.Compiler;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Runtime.Exceptions;

public class NativeStackFrame : IMTStackFrame
{
    private readonly MethodBase _method;
    private readonly string? _sourceName;
    private readonly int _lineNumber;

    public NativeStackFrame(MethodBase method, string? sourceName, int lineNumber)
    {
        _method = method;
        _sourceName = sourceName;
        _lineNumber = lineNumber;
    }

    public string MethodName => _method.Name;

    public string MethodSignature => _method.PrettyPrintNativeArgs();

    public string? MethodClass => _method.DeclaringType?.FullName;

    public JavaClass? MethodJavaClass => null;

    public Type? MethodNativeClass => _method.DeclaringType;

    public JavaMethodBody? JavaMethod => null;

    public int? OpcodeNumber => null;

    public int? LineNumber => _lineNumber == 0 ? null : _lineNumber;

    public bool IsBridge => _method.DeclaringType?.Name == CompilerUtils.BRIDGE_CLASS_NAME;

    public bool IsInterpreter => _method.DeclaringType == typeof(JavaRunner);

    public string? SourceFile
    {
        get
        {
            if (_sourceName == null)
                return null;
            string filename = _sourceName;
            if (filename != null && (filename.Contains('\\') || filename.Contains('/')))
            {
                int idx = filename.LastIndexOf('\\');
                filename = filename[((idx == -1 ? filename.LastIndexOf('/') : idx) + 1)..];
            }

            return filename;
        }
    }

    public override string ToString()
    {
        StringBuilder s = new StringBuilder();
        if (IsBridge)
        {
            s.Append("Bridge ");
            // bridges always have names "bridge_XYZ", we split out XYZ.
            s.Append(MethodName.Split('_')[1]);
            // that's all that matters. Bridges don't have sources, their signature is well-known.
            return s.ToString();
        }

        if (IsInterpreter)
        {
            // for interpreter, we want to know failed method and the line.
            s.Append(MethodName);
            // there are no overloaded methods. Signature is useless info.
            // () SHOULD be printed, but it looks weird to me so disabling for now
            //s.Append("()");
            // in RELEASE builds, this may be not available.
            if (LineNumber.HasValue)
            {
                // "(:123)" looks broken, " :123" looks weird.
                s.Append(", line ");
                s.Append(LineNumber.Value);
            }

            return s.ToString();
        }

        // For other cases we mimic what CLR does by default.
        s.Append(MethodClass);
        s.Append('.');
        s.Append(MethodName);
        s.Append(MethodSignature);
        if (LineNumber != null)
        {
            s.Append(" (");
            s.Append(SourceFile);
            if (LineNumber.HasValue)
            {
                s.Append(':');
                s.Append(LineNumber.Value);
            }

            s.Append(')');
        }

        // this SHOULD be printed, but the only consumer for now is frontend and it must use its own ways to show the difference.
        // When printed to plain text, difference is still visible due to java-styled method descriptors.
        //s.Append(" (native)");
        return s.ToString();
    }
}
