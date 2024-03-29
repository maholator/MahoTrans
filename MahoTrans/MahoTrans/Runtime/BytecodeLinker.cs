// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;
using MahoTrans.Compiler;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public static class BytecodeLinker
{
    public static void Link(JavaClass cls)
    {
        foreach (var method in cls.Methods.Values)
        {
            // we don't have bytecode.
            if (method.JavaBody == null)
                continue;

            // we won't be called
            if (method.IsAbstract || method.IsNative)
                continue;

            Link(method.JavaBody);
        }
    }

    private static void Link(JavaMethodBody method)
    {
        var isClinit = method.Method.Descriptor == NameDescriptor.ClassInit;
        try
        {
            LinkInternal(method, isClinit);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException(
                $"Failed to link {method}", e);
        }
    }

    private static void LinkLocals(JavaMethodBody method, ref LinkedInstruction[] output)
    {
        var code = method.Code;
        var methodName = method.Method.Descriptor.ToString();
        var logger = JvmContext.Toolkit?.LoadLogger;
        List<LocalVariableType>[] locals = new List<LocalVariableType>[method.LocalsCount];

        for (int i = 0; i < method.LocalsCount; i++)
            locals[i] = new List<LocalVariableType>();

        for (int i = 0; i < code.Length; i++)
        {
            string opcode;
            if (code[i].Opcode == JavaOpcode.wide)
            {
                JavaOpcode real = (JavaOpcode)code[i].Args[0];
                opcode = real.ToString();
            }
            else
            {
                opcode = code[i].Opcode.ToString();
            }

            if (opcode.IndexOf("load", StringComparison.Ordinal) == 1 ||
                opcode.IndexOf("store", StringComparison.Ordinal) == 1)
            {
                char type = opcode[0];
                int index;
                if (opcode.IndexOf('_') != -1)
                {
                    index = int.Parse(opcode.Split('_')[1]);
                }
                else if (code[i].Opcode == JavaOpcode.wide)
                {
                    index = Combine(code[i].Args[1], code[i].Args[2]);
                }
                else
                {
                    index = code[i].Args[0];
                }

                if (index >= method.LocalsCount)
                {
                    output[i] = new LinkedInstruction(MTOpcode.error_bytecode);
                    logger?.Log(LoadIssueType.LocalVariableIndexOutOfBounds, method.Method.Class.Name,
                        $"Local variable {index} of type \"{(LocalVariableType)type}\" is out of bounds at {methodName}:{i}");
                    continue;
                }

                if (!locals[index].Contains((LocalVariableType)type))
                {
                    locals[index].Add((LocalVariableType)type);
                }
            }
        }

        PrimitiveType[] types = new PrimitiveType[method.LocalsCount];

        for (int i = 0; i < method.LocalsCount; i++)
        {
            if (locals[i].Count > 1)
            {
                locals[i].Sort();
                logger?.Log(LoadIssueType.MultiTypeLocalVariable, method.Method.Class.Name,
                    $"Local variable {i} has multiple types: {string.Join(", ", locals[i])} at {methodName}");
                types[i] = default;
            }
            else if (locals[i].Count == 1)
            {
                types[i] = LocalToPrimitive(locals[i][0]);
            }
            else
            {
                types[i] = default;
            }
        }

        method.LocalTypes = types;
    }

    private static bool CheckMethodExit(Instruction[] code, NameDescriptor method, JavaClass cls)
    {
        if (code.Length == 0)
            return true;
        var lastOpcode = code[^1].Opcode;

        if (lastOpcode.IsJumpOpcode())
            return true;

        JvmContext.Toolkit?.LoadLogger?.Log(LoadIssueType.NoReturn, cls.Name,
            $"{method}'s last instruction is {lastOpcode}, so this method does not return.");
        return false;
    }

    private static void LinkInternal(JavaMethodBody method, bool isClinit)
    {
        JavaClass cls = method.Method.Class;
        JvmState jvm = JvmContext.Jvm!;
        var logger = jvm.Toolkit.LoadLogger;
        {
            // let's deal with arguments sizes first.
            var primargs = DescriptorUtils.ParseMethodDescriptorAsPrimitives(method.Method.Descriptor.Descriptor).args;
            if (!method.Method.IsStatic)
            {
                primargs = new[] { PrimitiveType.Reference }.Concat(primargs).ToArray();
            }

            var argsSizes = new byte[primargs.Length];

            for (int i = 0; i < argsSizes.Length; i++)
            {
                argsSizes[i] = ((primargs[i] & PrimitiveType.Is64) != 0) ? (byte)2 : (byte)1;
            }

            method.ArgsSizes = argsSizes;
        }
        var code = method.Code;
        var consts = cls.Constants;
        var output = new LinkedInstruction[code.Length];
        var isLinked = new bool[code.Length];
        var predStackOutput = new PredictedStackState[code.Length];
        predStackOutput[0].StackBeforeExecution = Array.Empty<PrimitiveType>(); // we enter with empty stack
        var auxData = new IJavaEntity?[code.Length];

        // offsets cache
        // key is offset, value is instruction index
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        for (int i = 0; i < code.Length; i++)
            offsets[code[i].Offset] = i;

        // stack verify data
        EmulatedFrameStack emulatedStack = new(method.StackSize, method.Code);

        var entryPoints = new Stack<int>();

        foreach (var methodCatch in method.Catches)
        {
            entryPoints.Push(offsets[methodCatch.CatchStart]);
            predStackOutput[offsets[methodCatch.CatchStart]].StackBeforeExecution =
                new[] { PrimitiveType.Reference };
        }

        entryPoints.Push(0);

        void SetStack(int target)
        {
            if (target < 0 || target >= predStackOutput.Length)
            {
                logger?.Log(LoadIssueType.BrokenFlow, cls.Name,
                    $"Attempt to set stack for instruction {target} in method {method.Method}, but it has only {code.Length} instructions.");
                return;
            }

            var now = emulatedStack.ToArray();
            var was = predStackOutput[target].StackBeforeExecution;
            if (was == null!)
                predStackOutput[target].StackBeforeExecution = now;
            else if (!was.SequenceEqual(now))
                throw new StackMismatchException($"Stack mismatch at instruction {target}");
        }

        try
        {
            entryPointsLoop: ;

            while (entryPoints.Count != 0)
            {
                // checking, did we pass this point already
                var entryPoint = entryPoints.Pop();
                if (isLinked[entryPoint])
                    continue;

                // bringing stack to valid state
                emulatedStack.Clear();
                var stackOnEntry = predStackOutput[entryPoint].StackBeforeExecution;
                if (stackOnEntry == null)
                    throw new JavaLinkageException($"Method can't be entered at {entryPoint}");
                foreach (var el in stackOnEntry)
                    emulatedStack.Push(el);

                for (int instrIndex = entryPoint; instrIndex < code.Length; instrIndex++)
                {
                    var instruction = code[instrIndex];
                    var args = instruction.Args;

                    // data to put into instruction
                    object data = null!;
                    int intData = 0;
                    ushort shortData = 0;
                    MTOpcode opcode = MTOpcode.error_bytecode;

                    emulatedStack.InstrIndex = instrIndex;

                    switch (instruction.Opcode)
                    {
                        case JavaOpcode.nop:
                            opcode = MTOpcode.nop;
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aconst_null:
                            opcode = MTOpcode.aconst_0;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_m1:
                            opcode = MTOpcode.iconst_m1;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_0:
                            opcode = MTOpcode.iconst_0;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_1:
                            opcode = MTOpcode.iconst_1;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_2:
                            opcode = MTOpcode.iconst_2;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_3:
                            opcode = MTOpcode.iconst_3;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_4:
                            opcode = MTOpcode.iconst_4;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iconst_5:
                            opcode = MTOpcode.iconst_5;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lconst_0:
                            opcode = MTOpcode.lconst_0;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lconst_1:
                            opcode = MTOpcode.lconst_1;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fconst_0:
                            opcode = MTOpcode.fconst_0;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fconst_1:
                            opcode = MTOpcode.fconst_1;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fconst_2:
                            opcode = MTOpcode.fconst_2;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dconst_0:
                            opcode = MTOpcode.dconst_0;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dconst_1:
                            opcode = MTOpcode.dconst_1;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.bipush:
                            opcode = MTOpcode.iconst;
                            intData = unchecked((sbyte)args[0]);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.sipush:
                            opcode = MTOpcode.iconst;
                            intData = Combine(args[0], args[1]);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ldc:
                        case JavaOpcode.ldc_w:
                        {
                            var index = instruction.Opcode == JavaOpcode.ldc ? args[0] : Combine(args[0], args[1]);
                            var obj = consts[index];
                            if (obj is int i)
                            {
                                emulatedStack.Push(PrimitiveType.Int);
                                opcode = MTOpcode.iconst;
                                intData = i;
                            }
                            else if (obj is float f)
                            {
                                emulatedStack.Push(PrimitiveType.Float);
                                opcode = MTOpcode.fconst;
                                intData = BitConverter.SingleToInt32Bits(f);
                            }
                            else if (obj is string s)
                            {
                                emulatedStack.Push(PrimitiveType.Reference);
                                opcode = MTOpcode.strconst;
                                data = s;
                            }
                            else
                            {
                                emulatedStack.PushError();
                                var msg =
                                    $"ldc opcode accepts int, float and text, but {obj.GetType()} given at index {index}";
                                logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg);
                                opcode = MTOpcode.error_bytecode;
                                data = msg;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.ldc2_w:
                        {
                            var index = Combine(args[0], args[1]);
                            var obj = consts[index];
                            if (obj is long l)
                            {
                                emulatedStack.Push(PrimitiveType.Long);
                                if (l == 0L)
                                    opcode = MTOpcode.lconst_0;
                                else if (l == 1L)
                                    opcode = MTOpcode.lconst_1;
                                else if (l == 2L)
                                    opcode = MTOpcode.lconst_2;
                                else
                                {
                                    opcode = MTOpcode.lconst;
                                    data = l;
                                }
                            }
                            else if (obj is double d)
                            {
                                emulatedStack.Push(PrimitiveType.Double);
                                if (d == 0d)
                                    opcode = MTOpcode.dconst_0;
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                else if (d == 1d)
                                    opcode = MTOpcode.dconst_1;
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                else if (d == 2d)
                                    opcode = MTOpcode.dconst_2;
                                else
                                {
                                    opcode = MTOpcode.dconst;
                                    data = d;
                                }
                            }
                            else
                            {
                                emulatedStack.PushError();
                                var msg =
                                    $"ldc2_w opcode accepts int, float and text, but {obj.GetType()} given at index {index}";
                                logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg);
                                opcode = MTOpcode.error_bytecode;
                                data = msg;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.iload:
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Int;
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lload:
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Long;
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fload:
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Float;
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dload:
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Double;
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aload:
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Reference;
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iload_0:
                            opcode = MTOpcode.load;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iload_1:
                            opcode = MTOpcode.load;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iload_2:
                            opcode = MTOpcode.load;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iload_3:
                            opcode = MTOpcode.load;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lload_0:
                            opcode = MTOpcode.load;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lload_1:
                            opcode = MTOpcode.load;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lload_2:
                            opcode = MTOpcode.load;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lload_3:
                            opcode = MTOpcode.load;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fload_0:
                            opcode = MTOpcode.load;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fload_1:
                            opcode = MTOpcode.load;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fload_2:
                            opcode = MTOpcode.load;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fload_3:
                            opcode = MTOpcode.load;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dload_0:
                            opcode = MTOpcode.load;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dload_1:
                            opcode = MTOpcode.load;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dload_2:
                            opcode = MTOpcode.load;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dload_3:
                            opcode = MTOpcode.load;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aload_0:
                            opcode = MTOpcode.load;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aload_1:
                            opcode = MTOpcode.load;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aload_2:
                            opcode = MTOpcode.load;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aload_3:
                            opcode = MTOpcode.load;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iaload:
                            opcode = MTOpcode.iaload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetInt);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.laload:
                            opcode = MTOpcode.laload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetLong);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.faload:
                            opcode = MTOpcode.faload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetFloat);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.daload:
                            opcode = MTOpcode.daload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetDouble);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aaload:
                            opcode = MTOpcode.aaload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetRef);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.baload:
                            opcode = MTOpcode.baload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetByte);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.caload:
                            opcode = MTOpcode.caload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetChar);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.saload:
                            opcode = MTOpcode.saload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetShort);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.istore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.astore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.istore_0:
                            opcode = MTOpcode.store;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.istore_1:
                            opcode = MTOpcode.store;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.istore_2:
                            opcode = MTOpcode.store;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.istore_3:
                            opcode = MTOpcode.store;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Int;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lstore_0:
                            opcode = MTOpcode.store;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lstore_1:
                            opcode = MTOpcode.store;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lstore_2:
                            opcode = MTOpcode.store;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lstore_3:
                            opcode = MTOpcode.store;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Long;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fstore_0:
                            opcode = MTOpcode.store;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fstore_1:
                            opcode = MTOpcode.store;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fstore_2:
                            opcode = MTOpcode.store;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fstore_3:
                            opcode = MTOpcode.store;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Float;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dstore_0:
                            opcode = MTOpcode.store;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dstore_1:
                            opcode = MTOpcode.store;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dstore_2:
                            opcode = MTOpcode.store;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dstore_3:
                            opcode = MTOpcode.store;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Double;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.astore_0:
                            opcode = MTOpcode.store;
                            intData = 0;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.astore_1:
                            opcode = MTOpcode.store;
                            intData = 1;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.astore_2:
                            opcode = MTOpcode.store;
                            intData = 2;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.astore_3:
                            opcode = MTOpcode.store;
                            intData = 3;
                            shortData = (ushort)PrimitiveType.Reference;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iastore:
                            opcode = MTOpcode.iastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetInt);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lastore:
                            opcode = MTOpcode.lastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetLong);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fastore:
                            opcode = MTOpcode.fastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetFloat);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dastore:
                            opcode = MTOpcode.dastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetDouble);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.aastore:
                            opcode = MTOpcode.aastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetRef);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.bastore:
                            opcode = MTOpcode.bastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetByte);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.castore:
                            opcode = MTOpcode.castore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetChar);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.sastore:
                            opcode = MTOpcode.sastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ArrayTargetShort);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.pop:
                            opcode = MTOpcode.pop;
                            PrimitiveType[] expected =
                            {
                                PrimitiveType.Int, PrimitiveType.Float, PrimitiveType.Reference,
                                PrimitiveType.SubroutinePointer
                            };
                            emulatedStack.PopWithAssert(expected, StackValuePurpose.Consume);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.pop2:
                        {
                            var t = emulatedStack.Pop(StackValuePurpose.Consume);
                            if ((t & PrimitiveType.Is64) != 0)
                            {
                                opcode = MTOpcode.pop;
                            }
                            else
                            {
                                emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                opcode = MTOpcode.pop2;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup:
                        {
                            opcode = MTOpcode.dup;
                            var t = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            emulatedStack.Push(t);
                            emulatedStack.Push(t);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup_x1:
                        {
                            opcode = MTOpcode.dup_x1;
                            var t1 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            var t2 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup_x2:
                        {
                            data = null!;
                            var t1 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            var t2 = emulatedStack.Pop(StackValuePurpose.Consume);
                            if ((t2 & PrimitiveType.Is64) != 0)
                            {
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x1;
                            }
                            else
                            {
                                var t3 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t3);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x2;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup2:
                        {
                            data = null!;
                            var t1 = emulatedStack.Pop(StackValuePurpose.Consume);
                            if ((t1 & PrimitiveType.Is64) != 0)
                            {
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup;
                            }
                            else
                            {
                                var t2 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup2;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup2_x1:
                        {
                            data = null!;
                            var t1 = emulatedStack.Pop(StackValuePurpose.Consume);

                            if ((t1 & PrimitiveType.Is64) != 0)
                            {
                                var t2 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x1;
                            }
                            else
                            {
                                var t2 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                var t3 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t3);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup2_x1;
                                intData = 0;
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.dup2_x2:
                            throw new NotImplementedException("No dup2_x2 opcode");
                        case JavaOpcode.swap:
                        {
                            opcode = MTOpcode.swap;
                            var t1 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            var t2 = emulatedStack.PopWithAssertIs32(StackValuePurpose.Consume);
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t2);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.iadd:
                            opcode = MTOpcode.iadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ladd:
                            opcode = MTOpcode.ladd;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fadd:
                            opcode = MTOpcode.fadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dadd:
                            opcode = MTOpcode.dadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.isub:
                            opcode = MTOpcode.isub;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lsub:
                            opcode = MTOpcode.lsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fsub:
                            opcode = MTOpcode.fsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dsub:
                            opcode = MTOpcode.dsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.imul:
                            opcode = MTOpcode.imul;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lmul:
                            opcode = MTOpcode.lmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fmul:
                            opcode = MTOpcode.fmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dmul:
                            opcode = MTOpcode.dmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.idiv:
                            opcode = MTOpcode.idiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ldiv:
                            opcode = MTOpcode.ldiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fdiv:
                            opcode = MTOpcode.fdiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ddiv:
                            opcode = MTOpcode.ddiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.irem:
                            opcode = MTOpcode.irem;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lrem:
                            opcode = MTOpcode.lrem;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.frem:
                            opcode = MTOpcode.frem;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.drem:
                            opcode = MTOpcode.drem;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ineg:
                            opcode = MTOpcode.ineg;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lneg:
                            opcode = MTOpcode.lneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fneg:
                            opcode = MTOpcode.fneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dneg:
                            opcode = MTOpcode.dneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ishl:
                            opcode = MTOpcode.ishl;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lshl:
                            opcode = MTOpcode.lshl;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ishr:
                            opcode = MTOpcode.ishr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lshr:
                            opcode = MTOpcode.lshr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iushr:
                            opcode = MTOpcode.iushr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lushr:
                            opcode = MTOpcode.lushr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iand:
                            opcode = MTOpcode.iand;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.land:
                            opcode = MTOpcode.land;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ior:
                            opcode = MTOpcode.ior;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lor:
                            opcode = MTOpcode.lor;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ixor:
                            opcode = MTOpcode.ixor;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lxor:
                            opcode = MTOpcode.lxor;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.iinc:
                            // sbyte cast here MUST BE because single-byte iinc may contain negative value, i.e. i-=1 is IINC 0x01 0xFF
                            intData = (sbyte)args[1];
                            shortData = args[0];
                            opcode = MTOpcode.iinc;
                            // no changes on stack
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2l:
                            opcode = MTOpcode.i2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2f:
                            opcode = MTOpcode.i2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2d:
                            opcode = MTOpcode.i2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.l2i:
                            opcode = MTOpcode.l2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.l2f:
                            opcode = MTOpcode.l2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.l2d:
                            opcode = MTOpcode.l2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.f2i:
                            opcode = MTOpcode.f2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.f2l:
                            opcode = MTOpcode.f2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.f2d:
                            opcode = MTOpcode.f2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.d2i:
                            opcode = MTOpcode.d2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.d2l:
                            opcode = MTOpcode.d2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.d2f:
                            opcode = MTOpcode.d2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2b:
                            opcode = MTOpcode.i2b;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2c:
                            opcode = MTOpcode.i2c;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.i2s:
                            opcode = MTOpcode.i2s;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.lcmp:
                            opcode = MTOpcode.lcmp;
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fcmpl:
                            opcode = MTOpcode.fcmpl;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.fcmpg:
                            opcode = MTOpcode.fcmpg;
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dcmpl:
                            opcode = MTOpcode.dcmpl;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.dcmpg:
                            opcode = MTOpcode.dcmpg;
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.ifeq:
                        {
                            opcode = MTOpcode.ifeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifne:
                        {
                            opcode = MTOpcode.ifne;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.iflt:
                        {
                            opcode = MTOpcode.iflt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifge:
                        {
                            opcode = MTOpcode.ifge;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifgt:
                        {
                            opcode = MTOpcode.ifgt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifle:
                        {
                            opcode = MTOpcode.ifle;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpeq:
                        {
                            opcode = MTOpcode.if_cmpeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpne:
                        {
                            opcode = MTOpcode.if_cmpne;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmplt:
                        {
                            opcode = MTOpcode.if_cmplt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpge:
                        {
                            opcode = MTOpcode.if_cmpge;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpgt:
                        {
                            opcode = MTOpcode.if_cmpgt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmple:
                        {
                            opcode = MTOpcode.if_cmple;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_acmpeq:
                        {
                            opcode = MTOpcode.if_cmpeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_acmpne:
                        {
                            opcode = MTOpcode.if_cmpne;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.@goto:
                        {
                            int target = CalcTargetInstruction();
                            SetStack(target);
                            SetDiff();
                            entryPoints.Push(target);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.jump, target);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.jsr:
                        case JavaOpcode.ret:
                            throw new NotImplementedException("No jsr/ret opcodes");
                        case JavaOpcode.tableswitch:
                        {
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            var i = 4 - ((instruction.Offset + 1) % 4);
                            if (i == 4)
                                i = 0;

                            int def = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                            i += 4; // default
                            int low = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                            i += 4; // low
                            int high = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                            i += 4; // high

                            int count = (high - low + 1);
                            int[] d = new int[3 + count];
                            d[0] = offsets[def + instruction.Offset];
                            d[1] = low;
                            d[2] = high;

                            SetStack(d[0]);

                            for (int j = 0; j < count; j++)
                            {
                                var off = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                                var next = offsets[off + instruction.Offset];
                                d[j + 3] = next;
                                SetStack(next);
                                entryPoints.Push(next);
                                i += 4;
                            }

                            data = d;
                            entryPoints.Push(d[0]);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.tableswitch, shortData, intData, data);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.lookupswitch:
                        {
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            var i = 4 - ((instruction.Offset + 1) % 4);
                            if (i == 4)
                                i = 0;
                            int def = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                            i += 4; // default

                            int count = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                            i += 4; //count

                            int[] d = new int[2 + count * 2];
                            d[0] = offsets[def + instruction.Offset];
                            d[1] = count;
                            SetStack(d[0]);
                            for (int j = 0; j < count; j++)
                            {
                                d[2 + j * 2] = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                                i += 4;
                                var off = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                                var next = offsets[off + instruction.Offset];
                                d[3 + j * 2] = next;
                                SetStack(next);
                                entryPoints.Push(next);
                                i += 4;
                            }

                            data = d;
                            entryPoints.Push(d[0]);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.lookupswitch, shortData, intData, data);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.ireturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.lreturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Long, StackValuePurpose.ToLocal);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.freturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Float, StackValuePurpose.ToLocal);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.dreturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Double, StackValuePurpose.ToLocal);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.areturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.ToLocal);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.@return:
                        {
                            SetDiff();
                            opcode = isClinit ? MTOpcode.return_void_inplace : MTOpcode.return_void;
                            output[instrIndex] = new LinkedInstruction(opcode);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.getstatic:
                        {
                            var f = getFieldSafely(cls, args, true, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                auxData[instrIndex] = f;
                                var index = jvm.StaticFieldsOwners.IndexOf(f);
                                if (index < 0)
                                {
                                    // maybe it's a native field?
                                    if (f.GetValue == null)
                                    {
                                        // no, it's not.
                                        throw new JavaLinkageException($"Static field {d} has no static slot!");
                                    }

                                    opcode = MTOpcode.bridge_init;
                                    intData = 1;
                                    data = new ClassBoundBridge(f.GetValue, c);
                                }
                                else if (c == cls)
                                {
                                    // field is in caller class. At this point initializer must be already run (method call without initializer is UB)
                                    opcode = MTOpcode.get_static;
                                    intData = index;
                                }
                                else
                                {
                                    opcode = MTOpcode.get_static_init;
                                    intData = index;
                                    data = c;
                                }
                            }

                            if (d != default)
                                emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.putstatic:
                        {
                            emulatedStack.Pop(StackValuePurpose.FieldValue); // TODO check field type
                            var f = getFieldSafely(cls, args, true, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                auxData[instrIndex] = f;
                                // set to "native" static is forbidden in MIDP/CLDC specs. we do not check this case.
                                var index = jvm.StaticFieldsOwners.IndexOf(f);
                                if (index < 0)
                                    throw new JavaLinkageException($"Static field {d} has no static slot!");

                                if (c == cls)
                                {
                                    // field is in caller class. At this point initializer must be already run (method call without initializer is UB)
                                    opcode = MTOpcode.set_static;
                                    intData = index;
                                }
                                else
                                {
                                    intData = index;
                                    data = c;
                                    opcode = MTOpcode.set_static_init;
                                }
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.getfield:
                        {
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Target);
                            var f = getFieldSafely(cls, args, false, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                auxData[instrIndex] = f;
                                var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                                if (c == cls)
                                {
                                    // field is in caller class. At this point initializer must be already run (method call without initializer is UB)
                                    opcode = MTOpcode.bridge;
                                    intData = 1;
                                    data = b;
                                }
                                else
                                {
                                    opcode = MTOpcode.bridge_init;
                                    intData = 1;
                                    data = new ClassBoundBridge(b, c);
                                }
                            }

                            if (d != default)
                                emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.putfield:
                        {
                            emulatedStack.Pop(StackValuePurpose.FieldValue); // TODO check field type
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Target);
                            var f = getFieldSafely(cls, args, false, ref opcode, ref data, out _, out var c);
                            if (f != null)
                            {
                                auxData[instrIndex] = f;
                                var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                                if (c == cls)
                                {
                                    // field is in caller class. At this point initializer must be already run (method call without initializer is UB)
                                    opcode = MTOpcode.bridge;
                                    intData = 2;
                                    data = b;
                                }
                                else
                                {
                                    opcode = MTOpcode.bridge_init;
                                    intData = 2;
                                    data = new ClassBoundBridge(b, c);
                                }
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.invokevirtual:
                        case JavaOpcode.invokeinterface:
                        {
                            var nd = LinkVirtualCall(cls, args, ref opcode, ref data, ref intData, ref shortData,
                                out var m);

                            auxData[instrIndex] = m;

                            if (nd == default)
                            {
                                throw new StackMismatchException(
                                    "Unable to predict stack state when no method signature is known.");
                            }

                            var d = DescriptorUtils.ParseMethodDescriptorAsPrimitives(nd.Descriptor);

                            foreach (var p in d.args.Reverse())
                                emulatedStack.PopWithAssert(p, StackValuePurpose.MethodArg);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Target);
                            if (d.returnType.HasValue) emulatedStack.Push(d.returnType.Value);

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.invokespecial:
                        case JavaOpcode.invokestatic:
                        {
                            bool isStatic = instruction.Opcode == JavaOpcode.invokestatic;
                            var m = getMethodSafely(cls, args, isStatic, ref opcode, ref data, out var ndc);
                            if (m != null)
                            {
                                auxData[instrIndex] = m;
                                MTOpcode op;
                                if (canCallSimply(m, cls))
                                    op = isStatic ? MTOpcode.invoke_static_simple : MTOpcode.invoke_instance_simple;
                                else
                                    op = isStatic ? MTOpcode.invoke_static : MTOpcode.invoke_instance;

                                if (m.Bridge == null)
                                {
                                    opcode = op;
                                    data = m;
                                }
                                else
                                {
                                    if (canCallSimply(m, cls))
                                    {
                                        opcode = MTOpcode.bridge;
                                        intData = m.ArgsCount + (isStatic ? 0 : 1);
                                        data = m.Bridge;
                                    }
                                    else
                                    {
                                        if (m.IsCritical)
                                            throw new JavaLinkageException("Critical bridges are not supported.");

                                        opcode = MTOpcode.bridge_init;
                                        intData = m.ArgsCount + (isStatic ? 0 : 1);
                                        data = new ClassBoundBridge(m.Bridge, m.Class);
                                    }
                                }
                            }

                            if (ndc != default)
                            {
                                var d = DescriptorUtils.ParseMethodDescriptorAsPrimitives(ndc.Descriptor.Descriptor);
                                foreach (var p in d.args.Reverse())
                                    emulatedStack.PopWithAssert(p, StackValuePurpose.MethodArg);
                                if (!isStatic)
                                    emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Target);

                                if (d.returnType.HasValue)
                                    emulatedStack.Push(d.returnType.Value);
                            }
                            else
                            {
                                throw new StackMismatchException(
                                    "Unable to predict stack state when no method signature is known.");
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.invokedynamic:
                            throw new NotImplementedException("No invokedynamic support");
                        case JavaOpcode.newobject:
                        {
                            if (getConstantSafely(cls, Combine(args[0], args[1]), ref opcode, ref data,
                                    out string type))
                            {
                                if (jvm.TryGetLoadedClass(type, out var cls1))
                                {
                                    opcode = MTOpcode.new_obj;
                                    data = cls1;
                                    auxData[instrIndex] = cls1;
                                }
                                else
                                {
                                    logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                                        $"\"{type}\" can't be found but going to be instantiated");
                                    opcode = MTOpcode.error_no_class;
                                    data = type;
                                }
                            }

                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.newarray:
                            opcode = MTOpcode.new_prim_arr;
                            intData = (int)(ArrayType)args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.anewarray:
                        {
                            opcode = MTOpcode.new_arr;
                            var type = (string)consts[Combine(args[0], args[1])];
                            string arrType;
                            if (type.StartsWith('['))
                                arrType = '[' + type;
                            else
                                arrType = $"[L{type};";
                            var arrCls = jvm.GetClass(arrType);
                            data = arrCls;
                            auxData[instrIndex] = arrCls;
                            emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.arraylength:
                            opcode = MTOpcode.array_length;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.athrow:
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            SetDiff();
                            output[instrIndex] = new LinkedInstruction(MTOpcode.athrow);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.checkcast:
                        {
                            if (getConstantSafely(cls, Combine(args[0], args[1]), ref opcode, ref data,
                                    out string type))
                            {
                                var cls1 = jvm.GetClassOrNull(type);
                                if (cls1 != null)
                                {
                                    opcode = MTOpcode.checkcast;
                                    data = cls1;
                                    auxData[instrIndex] = cls1;
                                }
                                else
                                {
                                    logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                                        $"\"{type}\" can't be found but going to be casted into");
                                    opcode = MTOpcode.error_no_class;
                                    data = type;
                                }
                            }

                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.instanceof:
                        {
                            if (getConstantSafely(cls, Combine(args[0], args[1]), ref opcode, ref data,
                                    out string type))
                            {
                                var cls1 = jvm.GetClassOrNull(type);
                                if (cls1 != null)
                                {
                                    opcode = MTOpcode.instanceof;
                                    data = cls1;
                                    auxData[instrIndex] = cls1;
                                }
                                else
                                {
                                    logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                                        $"\"{type}\" can't be found but going to be casted into");
                                    opcode = MTOpcode.error_no_class;
                                    data = type;
                                }
                            }

                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.monitorenter:
                            opcode = MTOpcode.monitor_enter;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.monitorexit:
                            opcode = MTOpcode.monitor_exit;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            SetNextStackAndDiff();
                            break;
                        case JavaOpcode.wide:
                        {
                            var op = (JavaOpcode)args[0];
                            if (op == JavaOpcode.iinc)
                            {
                                opcode = MTOpcode.iinc;
                                shortData = (ushort)Combine(args[1], args[2]);
                                // combine already gives us a signed short, nothing to do
                                intData = Combine(args[3], args[4]);
                            }
                            else
                            {
                                intData = Combine(args[1], args[2]);
                                switch (op)
                                {
                                    case JavaOpcode.aload:
                                        opcode = MTOpcode.load;
                                        emulatedStack.Push(PrimitiveType.Reference);
                                        break;
                                    case JavaOpcode.iload:
                                        opcode = MTOpcode.load;
                                        emulatedStack.Push(PrimitiveType.Int);
                                        break;
                                    case JavaOpcode.lload:
                                        opcode = MTOpcode.load;
                                        emulatedStack.Push(PrimitiveType.Long);
                                        break;
                                    case JavaOpcode.fload:
                                        opcode = MTOpcode.load;
                                        emulatedStack.Push(PrimitiveType.Float);
                                        break;
                                    case JavaOpcode.dload:
                                        opcode = MTOpcode.load;
                                        emulatedStack.Push(PrimitiveType.Double);
                                        break;
                                    case JavaOpcode.astore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Reference,
                                            StackValuePurpose.ToLocal);
                                        break;
                                    case JavaOpcode.istore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.ToLocal);
                                        break;
                                    case JavaOpcode.lstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Long,
                                            StackValuePurpose.ToLocal);
                                        break;
                                    case JavaOpcode.fstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Float,
                                            StackValuePurpose.ToLocal);
                                        break;
                                    case JavaOpcode.dstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Double,
                                            StackValuePurpose.ToLocal);
                                        break;
                                    default:
                                        throw new JavaRuntimeError($"Invalid wide opcode {op}");
                                }
                            }

                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.multianewarray:
                        {
                            opcode = MTOpcode.new_multi_arr;
                            var dims = args[2];
                            var type = (string)consts[Combine(args[0], args[1])];
                            intData = dims;
                            data = auxData[instrIndex] = jvm.GetClass(type);
                            for (int i = 0; i < dims; i++)
                            {
                                if (type[i] != '[')
                                    throw new JavaLinkageException(
                                        $"Multiarray has invalid type: \"{type}\" for {dims} dimensions");
                                emulatedStack.PopWithAssert(PrimitiveType.Int, StackValuePurpose.Consume);
                            }

                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStackAndDiff();
                            break;
                        }
                        case JavaOpcode.ifnull:
                        {
                            opcode = MTOpcode.ifeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            data = null!;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifnonnull:
                        {
                            opcode = MTOpcode.ifne;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference, StackValuePurpose.Consume);
                            int target = CalcTargetInstruction();
                            intData = target;
                            data = null!;
                            SetNextStackAndDiff();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.goto_w:
                        case JavaOpcode.jsr_w:
                            throw new NotImplementedException("Wide jumps not implemented yet.");
                        case JavaOpcode.breakpoint:
                            throw new NotImplementedException("No breakpoint opcode!");
                        case JavaOpcode._inplacereturn:
                            throw new JavaLinkageException(
                                "inplacereturn opcode can be generated by linker but can't arrive in method body.");
                        case JavaOpcode._invokeany:
                            opcode = MTOpcode.invoke_virtual_void_no_args_bysig;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                    isLinked[instrIndex] = true;

                    int CalcTargetInstruction()
                    {
                        var ros = Combine(args[0], args[1]);
                        var os = ros + instruction.Offset;
                        if (!offsets.TryGetValue(os, out var opcodeNum))
                            ThrowInvalidOffset(os, ros);
                        return opcodeNum;
                    }

                    void ThrowInvalidOffset(int globalOffset, int relativeOffset)
                    {
                        var offsetsPrint = string.Join('\n',
                            offsets.Select(x => $"{x.Value}: {x.Key} ({code[x.Value].Opcode})"));
                        throw new BrokenFlowException(
                            $"There is no opcode at offset {globalOffset} (relative {relativeOffset}).\nAvailable offsets:\n{offsetsPrint}");
                    }

                    void SetNextStackAndDiff()
                    {
                        SetDiff();
                        SetStack(instrIndex + 1);
                    }

                    void SetDiff()
                    {
                        predStackOutput[instrIndex].ValuesPoppedOnExecution = emulatedStack.GetPoppedAndReset();
                    }
                }
            }
        }
        catch (StackMismatchException e)
        {
            logger?.Log(LoadIssueType.StackMismatch, cls.Name, $"Method {method.Method.Descriptor}: " + e.Message);
            stubCode(ref output, out predStackOutput);
        }
        catch (BrokenFlowException e)
        {
            logger?.Log(LoadIssueType.BrokenFlow, cls.Name, $"Method {method.Method.Descriptor}: " + e.Message);
            stubCode(ref output, out predStackOutput);
        }

        if (!CheckMethodExit(code, method.Method.Descriptor, cls))
        {
            output[^1] = new LinkedInstruction(MTOpcode.error_bytecode);
        }

        LinkLocals(method, ref output);

        method.LinkedCatches = LinkCatches(method.Catches, consts, offsets, jvm);
        method.LinkedCode = output;
        method.StackTypes = predStackOutput;
        method.UsedEntities = auxData;
    }

    private static void stubCode(ref LinkedInstruction[] output, out PredictedStackState[] stackBeforeInstruction)
    {
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = new LinkedInstruction(MTOpcode.error_bytecode);
        }

        stackBeforeInstruction = new PredictedStackState[output.Length];
    }

    private static NameDescriptor LinkVirtualCall(JavaClass caller, byte[] args, ref MTOpcode opcode,
        ref object instrData,
        ref int instrIntData, ref ushort argsCount, out Method? method)
    {
        var logger = JvmContext.Toolkit?.LoadLogger;
        var jvm = JvmContext.Jvm!;
        var index = Combine(args[0], args[1]);

        {
            if (getConstantSilently(caller, index, out NameDescriptor nonBoundNd))
            {
                // this is a non-bound call
                argsCount = (ushort)DescriptorUtils.ParseMethodArgsCount(nonBoundNd.Descriptor);
                instrIntData = jvm.GetVirtualPointer(nonBoundNd);
                opcode = MTOpcode.invoke_virtual;
                method = null;
                return nonBoundNd;
            }
        }

        if (!getConstantSafely(caller, index, ref opcode, ref instrData, out NameDescriptorClass ndc))
        {
            method = null;
            return default;
        }


        if (!jvm.TryGetLoadedClass(ndc.ClassName, out var virtHost))
        {
            var msg = $"\"{ndc.ClassName}\" can't be found but its method \"{ndc.Descriptor}\" is going to be used";
            logger?.Log(LoadIssueType.MissingClassAccess, caller.Name, msg);
            opcode = MTOpcode.error_no_class;
            instrData = ndc.ClassName;
            method = null;
            return ndc.Descriptor;
        }

        method = virtHost.GetMethodRecursiveOrNull(ndc.Descriptor);

        if (method == null || method.IsStatic)
        {
            var msg = $"\"{ndc.ClassName}\" has no method \"{ndc.Descriptor}\"";
            logger?.Log(LoadIssueType.MissingVirtualAccess, caller.Name, msg);
        }
        else if (jvm.IsClassFinal(virtHost))
        {
            // if call target is final, we can devirt the method.
            // checking if we calling the same class.
            bool simple = canCallSimply(method, virtHost);
            if (method.Bridge == null)
            {
                opcode = MTOpcode.invoke_instance;
                instrData = method;
            }
            else if (simple)
            {
                opcode = MTOpcode.bridge;
                instrIntData = method.ArgsCount + 1;
                instrData = method.Bridge;
            }
            else
            {
                opcode = MTOpcode.bridge_init;
                instrIntData = method.ArgsCount + 1;
                instrData = new ClassBoundBridge(method.Bridge, method.Class);
            }

            return ndc.Descriptor;
        }

        // if class is not final, doing regular virtcall.
        argsCount = (ushort)DescriptorUtils.ParseMethodArgsCount(ndc.Descriptor.Descriptor);
        instrIntData = jvm.GetVirtualPointer(ndc.Descriptor);
        opcode = MTOpcode.invoke_virtual;

        return ndc.Descriptor;
    }

    /// <summary>
    ///     Links exceptions table.
    /// </summary>
    /// <param name="raw">Raw table.</param>
    /// <param name="constants">Class constants.</param>
    /// <param name="offsets">Offsets map. Key is offset, value is index.</param>
    /// <param name="jvm">Jvm.</param>
    /// <returns>Linked table.</returns>
    private static JavaMethodBody.LinkedCatch[] LinkCatches(JavaMethodBody.Catch[] raw, object[] constants,
        Dictionary<int, int> offsets, JvmState jvm)
    {
        var result = new JavaMethodBody.LinkedCatch[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            var tryStart = offsets[c.TryStart];
            var catchStart = offsets[c.CatchStart];
            // try end is exclusive bound, so here is a little hack:
            int tryEnd;
            int rawTryEnd = c.TryEnd;
            while (true)
            {
                if (offsets.TryGetValue(rawTryEnd, out var index))
                {
                    // we found NEXT instruction
                    tryEnd = index - 1;
                    break;
                }

                rawTryEnd++;
                if (rawTryEnd == int.MaxValue)
                    throw new JavaLinkageException($"Broken catch section {i}");
            }

            JavaClass type;
            if (c.Type == 0)
                type = jvm.GetClass("java/lang/Throwable");
            else
            {
                var obj = constants[c.Type];
                if (obj is string s)
                {
                    type = jvm.GetClass(s);
                }
                else
                {
                    throw new JavaLinkageException(
                        $"Catch type constant {c.Type} was {obj.GetType()}, string expected");
                }
            }

            result[i] = new JavaMethodBody.LinkedCatch(tryStart, tryEnd, catchStart, type);
        }

        return result;
    }

    /// <summary>
    ///     Gets constant at attempts to cast it.
    /// </summary>
    /// <param name="cls">Class to get constant from.</param>
    /// <param name="index">Index of constant.</param>
    /// <param name="opcode">Opcode to set to "error" on fail.</param>
    /// <param name="data">Data to set error message to.</param>
    /// <param name="result">Constant value.</param>
    /// <typeparam name="T">Constant type.</typeparam>
    /// <returns>False on error. Opcode and data will be updated in such case.</returns>
    /// <remarks>
    ///     This is basically "(T)cls.Constants[index]" but with safety checks.
    /// </remarks>
    private static bool getConstantSafely<T>(JavaClass cls, int index, ref MTOpcode opcode, ref object data,
        out T result)
    {
        var logger = JvmContext.Toolkit?.LoadLogger;
        var constants = cls.Constants;
        if (index < 0 || index >= constants.Length)
        {
            var msg = $"Invalid constant index {index}";
            logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg);
            opcode = MTOpcode.error_bytecode;
            data = msg;
            result = default!;
            return false;
        }

        if (constants[index] is T t)
        {
            result = t;
            return true;
        }

        var msg2 = $"Expected {typeof(T)} constant at {index} but got {constants.GetType()}";
        logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg2);
        opcode = MTOpcode.error_bytecode;
        data = msg2;
        result = default!;
        return false;
    }

    private static bool getConstantSilently<T>(JavaClass cls, int index, out T result)
    {
        var constants = cls.Constants;
        if (index >= 0 && index < constants.Length)
        {
            if (constants[index] is T t)
            {
                result = t;
                return true;
            }
        }

        result = default!;
        return false;
    }

    /// <summary>
    ///     Gets field of a class. Checks everything that can be checked.
    /// </summary>
    /// <param name="cls">Class to look from. Constant table is taken from it.</param>
    /// <param name="args">Opcode arguments.</param>
    /// <param name="isStatic">True if we look for static field.</param>
    /// <param name="opcode">Reference to opcode.</param>
    /// <param name="data">Reference to opcode data.</param>
    /// <param name="ndc">Found (or not) NDC of the field.</param>
    /// <param name="c">Found (or not) class where the field is.</param>
    /// <returns>
    ///     Found field. Null in case of failure. If null is returned, <paramref name="opcode" /> and
    ///     <paramref name="data" /> are set and must not be touched.
    /// </returns>
    private static Field? getFieldSafely(JavaClass cls, byte[] args, bool isStatic, ref MTOpcode opcode,
        ref object data, out NameDescriptorClass ndc, out JavaClass c)
    {
        var logger = JvmContext.Toolkit?.LoadLogger;
        var jvm = JvmContext.Jvm!;
        var index = Combine(args[0], args[1]);

        if (!getConstantSafely(cls, index, ref opcode, ref data, out ndc))
        {
            c = null!;
            return null;
        }

        if (!jvm.TryGetLoadedClass(ndc.ClassName, out c!))
        {
            var msg = $"\"{ndc.ClassName}\" can't be found but its field \"{ndc.Descriptor}\" is going to be used";
            logger?.Log(LoadIssueType.MissingClassAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_class;
            data = ndc.ClassName;
            return null;
        }

        var r = c.GetFieldRecursiveOrNull(ndc.Descriptor);

        if (!r.HasValue)
        {
            var msg = $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"";
            logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_field;
            data = ndc.Descriptor.Name;
            return null;
        }

        (c, var f) = r.Value;

        if (f.Flags.HasFlag(FieldFlags.Static) != isStatic)
        {
            logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                $"\"{ndc.ClassName}\" has field \"{ndc.Descriptor}\", but it {(isStatic ? "is not" : "is")} static");
            opcode = MTOpcode.error_no_field;
            data = ndc.Descriptor.Name;
            return null;
        }

        return f;
    }

    private static Method? getMethodSafely(JavaClass cls, byte[] args, bool isStatic, ref MTOpcode opcode,
        ref object data, out NameDescriptorClass ndc)
    {
        var logger = JvmContext.Toolkit?.LoadLogger;
        var jvm = JvmContext.Jvm!;
        var index = Combine(args[0], args[1]);

        if (!getConstantSafely(cls, index, ref opcode, ref data, out ndc))
        {
            return null;
        }

        if (!jvm.TryGetLoadedClass(ndc.ClassName, out var c))
        {
            var msg = $"\"{ndc.ClassName}\" can't be found but its method \"{ndc.Descriptor}\" is going to be used";
            logger?.Log(LoadIssueType.MissingClassAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_class;
            data = ndc.ClassName;
            return null;
        }

        var m = c.GetMethodRecursiveOrNull(ndc.Descriptor);

        if (m == null)
        {
            var msg = $"\"{ndc.ClassName}\" has no method \"{ndc.Descriptor}\"";
            logger?.Log(LoadIssueType.MissingMethodAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_method;
            data = ndc.Descriptor.Name;
            return null;
        }

        if (m.IsStatic != isStatic)
        {
            logger?.Log(LoadIssueType.MissingMethodAccess, cls.Name,
                $"\"{ndc.ClassName}\" has method \"{ndc.Descriptor}\", but it {(isStatic ? "is not" : "is")} static");
            opcode = MTOpcode.error_no_method;
            data = ndc.Descriptor.Name;
            return null;
        }

        return m;
    }

    private static bool canCallSimply(Method m, JavaClass callSource)
    {
        if (m.IsCritical)
            return false;
        return m.Class == callSource || m.Class.ClassInitMethod == null;
    }

    private static int Combine(byte indexByte1, byte indexByte2) => (short)(ushort)((indexByte1 << 8) | indexByte2);

    private enum LocalVariableType : ushort
    {
        Int = 'i',
        Long = 'l',
        Float = 'f',
        Double = 'd',
        Reference = 'a',
    }

    private static PrimitiveType LocalToPrimitive(LocalVariableType t)
    {
        return t switch
        {
            LocalVariableType.Int => PrimitiveType.Int,
            LocalVariableType.Long => PrimitiveType.Long,
            LocalVariableType.Float => PrimitiveType.Float,
            LocalVariableType.Double => PrimitiveType.Double,
            LocalVariableType.Reference => PrimitiveType.Reference,
            _ => default
        };
    }

    /// <summary>
    ///     Tool to track types of data on stack and verify it.
    /// </summary>
    public class EmulatedFrameStack
    {
        private readonly Stack<PrimitiveType> _stack = new();

        private readonly int _maxLength;
        private readonly Instruction[] _code;

        public int InstrIndex;

        private List<StackValuePurpose> _popTargets = new();

        public StackValuePurpose[] GetPoppedAndReset()
        {
            var arr = _popTargets.AsEnumerable().Reverse().ToArray();
            _popTargets.Clear();
            return arr;
        }

        public EmulatedFrameStack(int maxLength, Instruction[] code)
        {
            _maxLength = maxLength;
            _code = code;
        }

        public void Push(PrimitiveType t)
        {
            _stack.Push(t);
            if (_stack.Count > _maxLength)
                throw new StackMismatchException(
                    $"{Current()} attempts to overflow stack");
        }

        /// <summary>
        ///     Pushes unknown value to stack.
        /// </summary>
        public void PushError()
        {
            _stack.Push(default);
            if (_stack.Count > _maxLength)
                throw new StackMismatchException(
                    $"{Current()} attempts to overflow stack");
        }

        public PrimitiveType Pop(StackValuePurpose purp)
        {
            if (_stack.Count == 0)
                throw new StackMismatchException(
                    $"{Current()} attempts to pop from empty stack");
            _popTargets.Add(purp);
            return _stack.Pop();
        }

        public PrimitiveType PopWithAssertIs32(StackValuePurpose purp)
        {
            var real = Pop(purp);
            if (real == default)
                return real; // faulty instruction
            if ((real & PrimitiveType.Is64) != 0)
                throw new StackMismatchException(
                    $"{Current()} expects 32-bit value on stack but got {real}");
            return real;
        }

        public void PopWithAssert(PrimitiveType expected, StackValuePurpose purp)
        {
            var real = Pop(purp);
            if (real == default)
                return; // faulty instruction
            if (expected != real)
                throw new StackMismatchException($"{Current()} expects {expected} on stack but got {real}");
        }

        public void PopWithAssert(PrimitiveType[] expected, StackValuePurpose purp)
        {
            var real = Pop(purp);
            if (real == default)
                return; // faulty instruction
            if (!expected.Contains(real))
            {
                var expectedStr = expected.Length == 1
                    ? $"expects {expected[0]}"
                    : $"expects one of {string.Join(", ", expected)}";
                throw new StackMismatchException($"{Current()} {expectedStr} on stack but got {real}");
            }
        }

        private string Current() => $"Opcode {_code[InstrIndex].Opcode} at {InstrIndex}";

        public void Clear()
        {
            _stack.Clear();
            _popTargets.Clear();
        }

        public PrimitiveType[] ToArray() => _stack.Reverse().ToArray();
    }
}
