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

        if (CanCompile())
        {
            CompileInternal();
        }

        _fired = true;
    }

    private void CompileInternal()
    {
    }


    /// <summary>
    ///     Finds out can this method be compiled or not.
    /// </summary>
    /// <returns>False if this won't be compiled.</returns>
    public bool CanCompile()
    {
        if (_method.LinkedCatches.Length != 0)
            // methods with try-catches may have VERY CURSED execution flow, i don't want to solve bugs related to that.
            return false;

        foreach (var instruction in _method.LinkedCode)
        {
            if (!CrossCompilerUtils.CanCompileMethodWith(instruction))
                return false;
        }

        return true;
    }
}