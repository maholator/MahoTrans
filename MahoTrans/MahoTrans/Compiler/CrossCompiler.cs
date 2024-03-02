// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection.Emit;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

/// <summary>
///     Alternative to <see cref="JavaRunner" /> - instead of "doing" opcodes, recompiles them to IL.
/// </summary>
public class CrossCompiler
{
    private readonly JavaMethodBody _method;

    /// <summary>
    /// Host where compiled method will be hosted.
    /// </summary>
    private readonly ModuleBuilder _host;

    private TypeBuilder _builder = null!;
    private ILGenerator _il = null!;
    private bool _fired;

    // java moment: there must be paddings in locals after long vars, but it's okay to put double into ref local.
    // So tracking BOTH INDEX AND TYPE.
    private Dictionary<(int, PrimitiveType), LocalBuilder> _locals = new();

    public CrossCompiler(JavaMethodBody method, ModuleBuilder host)
    {
        _method = method;
        _host = host;
    }

    public void Compile()
    {
        if (_fired)
            throw new InvalidOperationException();

        if (true)
        {
            CompileInternal();
        }

        _fired = true;
    }

    private void CompileInternal()
    {
    }
}