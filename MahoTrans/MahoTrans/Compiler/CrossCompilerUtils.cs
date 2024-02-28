// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Compiler;

public static class CrossCompilerUtils
{
    public static bool CanCompileSingleOpcode(in LinkedInstruction instruction)
    {
        var type = instruction.Opcode.GetOpcodeType();
        switch (type)
        {
            case OpcodeType.NoOp:
                return true;
            case OpcodeType.Constant:
                return true;
            case OpcodeType.Local:
                return true;
            case OpcodeType.Array:
                return true;
            case OpcodeType.Stack:
                return true;
            case OpcodeType.Math:
                return true;
            case OpcodeType.Conversion:
                return true;
            case OpcodeType.Compare:
                return false;
            case OpcodeType.Branch:
                return false;
            case OpcodeType.Jump:
                return false;
            case OpcodeType.Return:
                return false;
            case OpcodeType.Cast:
                return false;
            case OpcodeType.Bridge:
                return false;
            case OpcodeType.Throw:
                return true;
            case OpcodeType.Alloc:
                return true;
            case OpcodeType.Call:
                return false;
            case OpcodeType.VirtCall:
                return false;
            case OpcodeType.Static:
                return true;
            case OpcodeType.Monitor:
                return false;
            case OpcodeType.Initializer:
                return false;
            case OpcodeType.Error:
                return false;
            default:
                // invalid opcode
                return false;
        }
    }

    public static bool CanCompileMethodWith(in LinkedInstruction instruction)
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
                return true;

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

    public static List<Range> GetPossiblyCompilableRanges(JavaMethodBody jmb)
    {
        LinkedInstruction[] instructions = jmb.LinkedCode;

        List<Range> list = new();

        {
            int? begin = 0;

            for (int i = 0; i < instructions.Length; i++)
            {
                if (begin.HasValue)
                {
                    if (!CanCompileMethodWith(instructions[i]))
                    {
                        list.Add(new Range(begin.Value, i));
                        begin = null;
                    }
                }
                else
                {
                    if (CanCompileMethodWith(instructions[i]))
                    {
                        var stack = jmb.StackTypes[i];
                        // entering only if there is a prebuilt stack
                        if (stack.StackBeforeExecution != null)
                        {
                            // there must be 0 or 1 values on stack (i.e. only returned value)
                            if (stack.StackBeforeExecution.Length < 2)
                            {
                                begin = i;
                            }
                        }
                    }
                }
            }

            if (begin.HasValue)
                list.Add(new Range(begin.Value, instructions.Length));
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var r = list[i];
            var len = r.GetOffsetAndLength(instructions.Length).Length;

            if (len <= 3)
            {
                // too short ranges are bad.
                list.RemoveAt(i);
            }
        }

        return list;
    }
}