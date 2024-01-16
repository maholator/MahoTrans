// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Text;
using MahoTrans.Runtime.Types;

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

    public string MethodSignature
    {
        get
        {
            StringBuilder s = new StringBuilder();
            var p = _method.GetParameters();
            s.Append('(');
            for (int k = 0; k < p.Length; k++)
            {
                s.Append(p[k].ParameterType.Name);
                s.Append(' ');
                s.Append(p[k].Name);
                if (k + 1 != p.Length)
                    s.Append(", ");
            }

            s.Append(')');
            if (_method is MethodInfo mi)
            {
                s.Append(' ');
                s.Append(mi.ReturnType.Name);
            }

            return s.ToString();
        }
    }

    public string? MethodClass => _method.DeclaringType?.FullName;

    public JavaClass? MethodJavaClass => null;

    public Type? MethodNativeClass => _method.DeclaringType;

    public JavaMethodBody? JavaMethod => null;

    public int? OpcodeNumber => null;

    public int? LineNumber => _lineNumber == 0 ? null : _lineNumber;

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
        s.Append(MethodName);
        s.Append(MethodSignature);
        if (LineNumber != null)
        {
            s.Append('(');
            s.Append(SourceFile);
            s.Append(':');
            s.Append(LineNumber);
            s.Append(')');
        }
        s.Append(" (native)");
        return s.ToString();
    }
}