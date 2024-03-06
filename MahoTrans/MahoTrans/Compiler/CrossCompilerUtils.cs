// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;
using MahoTrans.Utils;

namespace MahoTrans.Compiler;

public static class CrossCompilerUtils
{
    /// <summary>
    ///     Finds out can this method be compiled or not.
    /// </summary>
    /// <returns>False if this must not be compiled.</returns>
    public static bool CanCompileMethodWith(this JavaMethodBody method)
    {
        if (method.LinkedCatches.Length != 0)
            // methods with try-catches may have VERY CURSED execution flow, i don't want to solve bugs related to that.
            return false;

        foreach (var instruction in method.LinkedCode)
        {
            if (!CanCompileMethodWith(instruction))
                return false;
        }

        return true;
    }

    public static bool CanCompileSingleOpcode(in LinkedInstruction instruction)
    {
        var opcode = instruction.Opcode;
        var type = opcode.GetOpcodeType();
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
                return opcode switch
                {
                    MTOpcode.pop => true,
                    MTOpcode.pop2 => true,
                    MTOpcode.swap => true,
                    MTOpcode.dup => true,
                    _ => false
                };
            case OpcodeType.Math:
                return true;
            case OpcodeType.Conversion:
                return false;
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

    public static List<CCRFR> GetPossiblyCompilableRanges(JavaMethodBody jmb)
    {
        LinkedInstruction[] instructions = jmb.LinkedCode;

        List<CCRFR> list = new();

        {
            int? begin = null;
            int maxStack = 0;
            PrimitiveType? stackOnEnter = default;

            for (int i = 0; i < instructions.Length; i++)
            {
                if (begin.HasValue)
                {
                    var currStack = jmb.StackTypes[i].StackBeforeExecution.Length;
                    maxStack = Math.Max(maxStack, currStack);
                    if (!CanCompileMethodWith(instructions[i]))
                    {
                        list.Add(new CCRFR
                        {
                            Start = begin.Value,
                            Length = i - begin.Value,
                            MaxStackSize = (ushort)maxStack,
                            StackOnEnter = stackOnEnter,
                            StackOnExit = (ushort)currStack
                        });
                        begin = null;
                    }
                }
                else
                {
                    if (CanCompileMethodWith(instructions[i]))
                    {
                        var stack = jmb.StackTypes[i];
                        // there must be 0 or 1 values on stack (i.e. only returned value)
                        var stackLenOnEnter = stack.StackBeforeExecution.Length;
                        if (stackLenOnEnter < 2)
                        {
                            stackOnEnter = stackLenOnEnter == 1 ? stack.StackBeforeExecution[0] : null;
                            begin = i;
                            maxStack = stackLenOnEnter;
                        }
                    }
                }
            }

            if (begin.HasValue)
            {
                list.Add(new CCRFR
                {
                    Start = begin.Value,
                    Length = instructions.Length - begin.Value - 1,
                    MaxStackSize = (ushort)maxStack,
                    StackOnEnter = stackOnEnter,
                    StackOnExit = 0,
                });
            }
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Length <= 3)
            {
                // too short ranges are bad.
                list.RemoveAt(i);
            }
        }

        return list;
    }

    /// <summary>
    ///     Attempts to predict purposes of values on stack.
    /// </summary>
    /// <param name="jmb">Method body.</param>
    /// <param name="ccrfr">Range to process.</param>
    /// <returns>
    ///     Stack map. Length of this is range.length+1. Element at index I represents stack before instruction with index
    ///     I in the range. The last element is stack state after last instruction (and before CCR exit). Each element is array
    ///     of purposes for each value. Indexing from zero. Array lengths are equal to stack sizes.
    /// </returns>
    public static StackValuePurpose[][] PredictPurposes(JavaMethodBody jmb, CCRFR ccrfr)
    {
        var purps = new StackValuePurpose[ccrfr.Length + 1][];

        // filling last element

        if (ccrfr.TerminatesMethod(jmb))
        {
            // we terminate the method
            purps[^1] = Array.Empty<StackValuePurpose>();
        }
        else
        {
            // there is something after us...
            var s = jmb.StackTypes[ccrfr.Start + ccrfr.Length].StackBeforeExecution;
            if (s == null)
                throw new ArgumentException();
            var arr = new StackValuePurpose[s.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = StackValuePurpose.ToLocal;
            }
        }

        // now iterating over code

        for (int i = ccrfr.Length - 1; i >= 0; i--)
        {
            var globalIndex = i + ccrfr.Start;

            var np = new StackValuePurpose[jmb.StackTypes[globalIndex].StackBeforeExecution.Length];
            purps[i] = np;
            // [ notTouched taken taken ]
            // consumed=2 left=1 np.len=3
            var consumed = jmb.StackTypes[globalIndex].ValuesPoppedOnExecution;
            var left = np.Length - consumed.Length;
            // coping untouched
            Array.Copy(purps[i + 1], 0, np, 0, left);
            // taken
            consumed.CopyTo(np, left);
        }

        return purps;
    }

    /// <summary>
    ///     Attempts to predict types of values on stack. This is known ahead of time, basically, this just copies known values
    ///     out.
    /// </summary>
    /// <param name="jmb">Method body.</param>
    /// <param name="ccrfr">Range to process.</param>
    /// <returns>
    ///     Stack map. Length of this is range.length+1. Element at index I represents stack before instruction with index
    ///     I in the range. The last element is stack state after last instruction (and before CCR exit). Each element is array
    ///     of each value "java primitive" types. Indexing from zero. Array lengths are equal to stack sizes.
    /// </returns>
    public static PrimitiveType[][] PredictTypes(JavaMethodBody jmb, CCRFR ccrfr)
    {
        var types = new PrimitiveType[ccrfr.Length + 1][];
        var seg = jmb.StackTypes[ccrfr];
        for (int i = 0; i < ccrfr.Length; i++)
        {
            types[i] = seg[i].StackBeforeExecution;
        }

        if (ccrfr.TerminatesMethod(jmb))
        {
            types[^1] = Array.Empty<PrimitiveType>();
        }
        else
        {
            types[^1] = jmb.StackTypes[ccrfr.EndExclusive].StackBeforeExecution;
        }

        return types;
    }

    public static Dictionary<int, int> PredictStackObject(StackValuePurpose[][] purps)
    {
        Dictionary<int, int> res = new();
        var valuesCount = purps[^1].Length;
        for (int i = 0; i < valuesCount; i++)
        {
            for (int j = purps.Length - 1; j >= 0; j--)
            {
                if (purps[j].Length <= i)
                {
                    res.Add(j+1, i);
                    goto end;
                }
            }

            res.Add(-1, i);

            end: ;
        }

        return res;
    }

    /// <summary>
    ///     Gets CLR type most suitable to represent value on stack, i.e. for
    ///     <see cref="PrimitiveType" />.<see cref="PrimitiveType.Int" /> gives <see cref="Int32" />.
    /// </summary>
    /// <param name="p">Type to convert.</param>
    /// <returns>CLR type most suitable to represent the value.</returns>
    public static Type ToType(this PrimitiveType p)
    {
        return p switch
        {
            PrimitiveType.Int => typeof(int),
            PrimitiveType.Long => typeof(long),
            PrimitiveType.Float => typeof(float),
            PrimitiveType.Double => typeof(double),
            PrimitiveType.Reference => typeof(Reference),
            _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
        };
    }

    public static Type ToArrayType(this StackValuePurpose p)
    {
        return p switch
        {
            StackValuePurpose.ArrayTargetByte => typeof(sbyte),
            StackValuePurpose.ArrayTargetChar => typeof(char),
            StackValuePurpose.ArrayTargetShort => typeof(short),
            StackValuePurpose.ArrayTargetInt => typeof(int),
            StackValuePurpose.ArrayTargetLong => typeof(long),
            StackValuePurpose.ArrayTargetFloat => typeof(float),
            StackValuePurpose.ArrayTargetDouble => typeof(double),
            StackValuePurpose.ArrayTargetRef => typeof(Reference),
            _ => throw new ArgumentOutOfRangeException(nameof(p), p, null)
        };
    }
}