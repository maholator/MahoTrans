// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Compiler;

/// <summary>
///     Alternative to <see cref="JavaRunner" /> - instead of "doing" opcodes, recompiles them to IL
/// </summary>
public class CrossCompiler
{
    private readonly JavaMethodBody _method;
    private readonly TypeBuilder _host;
    private bool _fired;

    // java moment: there must be paddings in locals after long vars, but it's okay to put double into ref local.
    // So tracking BOTH INDEX AND TYPE.
    private Dictionary<(int, PrimitiveType), LocalBuilder> _locals = new();

    public CrossCompiler(JavaMethodBody method, TypeBuilder host)
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


    public bool CanCompile()
    {
        if (_method.LinkedCatches.Length != 0)
            return false;

        foreach (var instruction in _method.LinkedCode)
        {
            var type = instruction.Opcode.GetOpcodeType();
            switch (type)
            {
                case OpcodeType.NoOp:
                case OpcodeType.Constant:
                case OpcodeType.Local:
                case OpcodeType.Array:
                case OpcodeType.Stack:
                case OpcodeType.Math:
                case OpcodeType.Conversion:
                case OpcodeType.Compare:
                case OpcodeType.Branch:
                case OpcodeType.Jump:
                case OpcodeType.Return:
                case OpcodeType.Cast:
                case OpcodeType.Bridge:
                case OpcodeType.Throw:
                case OpcodeType.Alloc:
                case OpcodeType.Call:
                case OpcodeType.VirtCall:
                case OpcodeType.Static:
                    break;

                case OpcodeType.Monitor:
                    // let's not now
                    return false;

                case OpcodeType.Initializer:
                    // we can do nothing with initializers.
                    return false;
                case OpcodeType.Error:
                    // method is broken.
                    return false;
                default:
                    // invalid opcode
                    return false;
            }
        }

        return true;
    }
}