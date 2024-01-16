// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Types;
using System.Text;

namespace MahoTrans.Runtime.Exceptions;

public class JavaStackFrame : IMTStackFrame
{
    private readonly Method _method;
    private readonly int _opcodeIndex;

    public JavaStackFrame(Frame frame)
    {
        _method = frame.Method.Method;
        _opcodeIndex = frame.Pointer;
    }

    public string MethodName => _method.Descriptor.Name;

    public string MethodSignature => _method.Descriptor.Descriptor;

    public string MethodClass => _method.Class.Name;

    public JavaClass MethodJavaClass => _method.Class;

    public Type? MethodNativeClass => _method.Class.ClrType;

    public JavaMethodBody JavaMethod => _method.JavaBody;

    public int? OpcodeNumber => _opcodeIndex;

    //TODO
    public int? LineNumber => null;

    public string? SourceFile => null;

    public new string ToString()
    {
        StringBuilder s = new StringBuilder();
        s.Append(MethodClass.Replace('/', '.'));
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
        else
        {
            s.Append(" bci=");
            s.Append(OpcodeNumber);
        }
        return s.ToString();
    }
}