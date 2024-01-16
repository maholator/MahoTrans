// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Text;
using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime.Exceptions;

public class NativeStackFrame : IMTStackFrame
{
    private readonly MethodBase _method;
    private readonly int _lineNumber;

    public NativeStackFrame(MethodBase method, int lineNumber)
    {
        _method = method;
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
}