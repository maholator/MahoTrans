// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using MahoTrans.Abstractions;
using MahoTrans.Native;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public static class BytecodeLinker
{
    public static void Link(JavaMethodBody method, JvmState jvm)
    {
        var isClinit = method.Method.Descriptor.Name == "<clinit>";
        try
        {
            LinkInternal(method, method.Method.Class, jvm, isClinit);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException(
                $"Failed to link {method} in JIT manner", e);
        }
    }

    public static void Verify(JavaClass cls, JvmState jvm)
    {
        var logger = jvm.Toolkit.LoadLogger;

        foreach (var method in cls.Methods.Values)
        {
            // we don't have bytecode at all.
            if (method.IsNative)
                continue;

            // we don't have bytecode because we have no implementation.
            if (method.IsAbstract)
                continue;

            // there was a try-catch but its hit is a failure by itself, soo...
            Instruction[] code = method.JavaBody.Code;

            VerifyClassReferences(code, cls, jvm, logger);
            VerifyLocals(method.JavaBody, cls.Name, logger);
            CheckMethodExit(code, method.Descriptor.ToString(), cls.Name, logger);
        }
    }

    /// <summary>
    ///     Checks that there is no broken references.
    /// </summary>
    private static void VerifyClassReferences(Instruction[] code, JavaClass cls, JvmState jvm, ILoadLogger? logger)
    {
        var consts = cls.Constants;

        foreach (var instruction in code)
        {
            var args = instruction.Args;
            switch (instruction.Opcode)
            {
                case JavaOpcode.newobject:
                {
                    var type = (string)consts[Combine(args[0], args[1])];
                    if (!jvm.Classes.ContainsKey(type))
                    {
                        logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{type}\" can't be found but going to be instantiated");
                    }

                    break;
                }
                case JavaOpcode.checkcast:
                case JavaOpcode.instanceof:
                {
                    var type = (string)consts[Combine(args[0], args[1])];
                    try
                    {
                        jvm.GetClass(type);
                    }
                    catch
                    {
                        logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{type}\" can't be found but going to be casted into");
                    }

                    break;
                }
                case JavaOpcode.invokespecial:
                case JavaOpcode.invokestatic:
                case JavaOpcode.invokeinterface:
                case JavaOpcode.invokevirtual:
                {
                    NameDescriptorClass ndc;
                    if (consts[Combine(args[0], args[1])] is NameDescriptorClass)
                    {
                        ndc = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    }
                    else if (consts[Combine(args[0], args[1])] is NameDescriptor)
                    {
                        break;
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.InvalidConstant, cls.Name,
                            $"Constant \"{Combine(args[0], args[1])}\" isn't a member reference");
                        break;
                    }

                    if (jvm.Classes.TryGetValue(ndc.ClassName, out var c))
                    {
                        if (c.IsInterface)
                        {
                            var attr = c.ClrType?.GetCustomAttribute<JavaInterfaceAttribute>();
                            if (attr == null)
                            {
                                // this is an interface from java code
                                break;
                            }

                            var reference = attr.ReferenceImplementation;
                            if (reference == null)
                            {
                                // can't check interface without reference implementation.
                                logger?.Log(LoadIssueType.QuestionableNativeCode, cls.Name,
                                    $"Interface \"{c.Name}\" should have reference implementation.");
                                break;
                            }

                            try
                            {
                                c = jvm.GetClass(reference.FullName!.Replace('.', '/'));
                            }
                            catch
                            {
                                logger?.Log(LoadIssueType.QuestionableNativeCode, cls.Name,
                                    $"Interface \"{c.Name}\" has invalid reference implementation.");
                                break;
                            }
                        }

                        try
                        {
                            c.GetMethodRecursive(ndc.Descriptor);
                        }
                        catch
                        {
                            logger?.Log(LoadIssueType.MissingMethodAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no method \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its method \"{ndc.Descriptor}\" will be used");
                    }

                    break;
                }
                case JavaOpcode.getfield:
                case JavaOpcode.putfield:
                {
                    NameDescriptorClass ndc;
                    if (consts[Combine(args[0], args[1])] is NameDescriptorClass)
                    {
                        ndc = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.InvalidConstant, cls.Name,
                            $"Constant \"{Combine(args[0], args[1])}\" isn't a member reference");
                        break;
                    }

                    if (jvm.Classes.TryGetValue(ndc.ClassName, out var c))
                    {
                        try
                        {
                            var f = c.GetFieldRecursive(ndc.Descriptor);
                            if (f.Flags.HasFlag(FieldFlags.Static))
                            {
                                logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                    $"\"{ndc.ClassName}\" has field \"{ndc.Descriptor}\", but it is static");
                            }
                        }
                        catch
                        {
                            logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its field \"{ndc.Descriptor}\" will be used");
                    }

                    break;
                }
                case JavaOpcode.getstatic:
                case JavaOpcode.putstatic:
                {
                    NameDescriptorClass ndc;

                    if (consts[Combine(args[0], args[1])] is NameDescriptorClass)
                    {
                        ndc = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.InvalidConstant, cls.Name,
                            $"Constant \"{Combine(args[0], args[1])}\" isn't a member reference");
                        break;
                    }

                    if (jvm.Classes.TryGetValue(ndc.ClassName, out var c))
                    {
                        try
                        {
                            var f = c.GetFieldRecursive(ndc.Descriptor);
                            if (!f.Flags.HasFlag(FieldFlags.Static))
                            {
                                logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                    $"\"{ndc.ClassName}\" has field \"{ndc.Descriptor}\", but it is not static");
                            }
                        }
                        catch
                        {
                            logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its field \"{ndc.Descriptor}\" will be used");
                    }

                    break;
                }
            }
        }
    }


    private static void VerifyLocals(JavaMethodBody method, string cls, ILoadLogger? logger)
    {
        var code = method.Code;
        var methodName = method.Method.Descriptor.ToString();
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
                    logger?.Log(LoadIssueType.LocalVariableIndexOutOfBounds, cls,
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
                logger?.Log(LoadIssueType.MultiTypeLocalVariable, cls,
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

    private static void CheckMethodExit(Instruction[] code, string method, string cls, ILoadLogger? logger)
    {
        if (code.Length == 0)
            return;
        var lastOpcode = code[^1].Opcode;

        switch (lastOpcode)
        {
            case JavaOpcode.@goto:
            case JavaOpcode.jsr:
            case JavaOpcode.ret:
            case JavaOpcode.tableswitch:
            case JavaOpcode.lookupswitch:
            case JavaOpcode.ireturn:
            case JavaOpcode.lreturn:
            case JavaOpcode.freturn:
            case JavaOpcode.dreturn:
            case JavaOpcode.areturn:
            case JavaOpcode.@return:
            case JavaOpcode.athrow:
            case JavaOpcode.goto_w:
            case JavaOpcode.jsr_w:
            case JavaOpcode._inplacereturn:
                return;
        }

        logger?.Log(LoadIssueType.BrokenFlow, cls,
            $"{method}'s last instruction is {lastOpcode}, which does not terminate the method.");
    }

    private static void LinkInternal(JavaMethodBody method, JavaClass cls, JvmState jvm, bool isClinit)
    {
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
                argsSizes[i] = ((primargs[i] & PrimitiveType.IsDouble) != 0) ? (byte)2 : (byte)1;
            }

            method.ArgsSizes = argsSizes;
        }
        var code = method.Code;
        var consts = cls.Constants;
        var output = new LinkedInstruction[code.Length];
        var isLinked = new bool[code.Length];
        PrimitiveType[]?[] stackBeforeInstruction = new PrimitiveType[]?[code.Length];
        stackBeforeInstruction[0] = Array.Empty<PrimitiveType>(); // we enter with empty stack

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
            stackBeforeInstruction[offsets[methodCatch.CatchStart]] = new[] { PrimitiveType.Reference };
        }

        entryPoints.Push(0);

        void SetStack(int target)
        {
            var now = emulatedStack.ToArray();
            var was = stackBeforeInstruction[target];
            if (was == null)
                stackBeforeInstruction[target] = now;
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
                var stackOnEntry = stackBeforeInstruction[entryPoint];
                if (stackOnEntry == null)
                    throw new JavaLinkageException($"Method can't be entered at {entryPoint}");
                foreach (var el in stackOnEntry.Reverse())
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
                            SetNextStack();
                            break;
                        case JavaOpcode.aconst_null:
                            opcode = MTOpcode.iconst_0;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_m1:
                            opcode = MTOpcode.iconst_m1;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_0:
                            opcode = MTOpcode.iconst_0;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_1:
                            opcode = MTOpcode.iconst_1;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_2:
                            opcode = MTOpcode.iconst_2;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_3:
                            opcode = MTOpcode.iconst_3;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_4:
                            opcode = MTOpcode.iconst_4;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iconst_5:
                            opcode = MTOpcode.iconst_5;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lconst_0:
                            opcode = MTOpcode.lconst_0;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lconst_1:
                            opcode = MTOpcode.lconst_1;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fconst_0:
                            opcode = MTOpcode.fconst_0;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fconst_1:
                            opcode = MTOpcode.fconst_1;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fconst_2:
                            opcode = MTOpcode.fconst_2;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dconst_0:
                            opcode = MTOpcode.dconst_0;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dconst_1:
                            opcode = MTOpcode.dconst_1;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.bipush:
                            opcode = MTOpcode.iconst;
                            intData = unchecked((sbyte)args[0]);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.sipush:
                            opcode = MTOpcode.iconst;
                            intData = Combine(args[0], args[1]);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
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
                                opcode = MTOpcode.iconst;
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
                                var msg =
                                    $"ldc opcode accepts int, float and text, but {obj.GetType()} given at index {index}";
                                logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg);
                                opcode = MTOpcode.error_bytecode;
                                data = msg;
                            }

                            SetNextStack();
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
                                var msg =
                                    $"ldc2_w opcode accepts int, float and text, but {obj.GetType()} given at index {index}";
                                logger?.Log(LoadIssueType.InvalidConstant, cls.Name, msg);
                                opcode = MTOpcode.error_bytecode;
                                data = msg;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.iload:
                            intData = args[0];
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lload:
                            intData = args[0];
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fload:
                            intData = args[0];
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dload:
                            intData = args[0];
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.aload:
                            intData = args[0];
                            opcode = MTOpcode.load;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.iload_0:
                            opcode = MTOpcode.load_0;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iload_1:
                            opcode = MTOpcode.load_1;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iload_2:
                            opcode = MTOpcode.load_2;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.iload_3:
                            opcode = MTOpcode.load_3;
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lload_0:
                            opcode = MTOpcode.load_0;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lload_1:
                            opcode = MTOpcode.load_1;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lload_2:
                            opcode = MTOpcode.load_2;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lload_3:
                            opcode = MTOpcode.load_3;
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fload_0:
                            opcode = MTOpcode.load_0;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fload_1:
                            opcode = MTOpcode.load_1;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fload_2:
                            opcode = MTOpcode.load_2;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fload_3:
                            opcode = MTOpcode.load_3;
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dload_0:
                            opcode = MTOpcode.load_0;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dload_1:
                            opcode = MTOpcode.load_1;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dload_2:
                            opcode = MTOpcode.load_2;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dload_3:
                            opcode = MTOpcode.load_3;
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.aload_0:
                            opcode = MTOpcode.load_0;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.aload_1:
                            opcode = MTOpcode.load_1;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.aload_2:
                            opcode = MTOpcode.load_2;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.aload_3:
                            opcode = MTOpcode.load_3;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.iaload:
                            opcode = MTOpcode.iaload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.laload:
                            opcode = MTOpcode.laload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.faload:
                            opcode = MTOpcode.faload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.daload:
                            opcode = MTOpcode.daload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.aaload:
                            opcode = MTOpcode.aaload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.baload:
                            opcode = MTOpcode.baload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.caload:
                            opcode = MTOpcode.caload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.saload:
                            opcode = MTOpcode.saload;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.istore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dstore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.astore:
                            opcode = MTOpcode.store;
                            intData = args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.istore_0:
                            opcode = MTOpcode.store_0;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.istore_1:
                            opcode = MTOpcode.store_1;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.istore_2:
                            opcode = MTOpcode.store_2;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.istore_3:
                            opcode = MTOpcode.store_3;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lstore_0:
                            opcode = MTOpcode.store_0;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lstore_1:
                            opcode = MTOpcode.store_1;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lstore_2:
                            opcode = MTOpcode.store_2;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.lstore_3:
                            opcode = MTOpcode.store_3;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fstore_0:
                            opcode = MTOpcode.store_0;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fstore_1:
                            opcode = MTOpcode.store_1;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fstore_2:
                            opcode = MTOpcode.store_2;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.fstore_3:
                            opcode = MTOpcode.store_3;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dstore_0:
                            opcode = MTOpcode.store_0;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dstore_1:
                            opcode = MTOpcode.store_1;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dstore_2:
                            opcode = MTOpcode.store_2;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.dstore_3:
                            opcode = MTOpcode.store_3;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.astore_0:
                            opcode = MTOpcode.store_0;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.astore_1:
                            opcode = MTOpcode.store_1;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.astore_2:
                            opcode = MTOpcode.store_2;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.astore_3:
                            opcode = MTOpcode.store_3;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.iastore:
                            opcode = MTOpcode.iastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.lastore:
                            opcode = MTOpcode.lastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.fastore:
                            opcode = MTOpcode.fastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.dastore:
                            opcode = MTOpcode.dastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.aastore:
                            opcode = MTOpcode.aastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.bastore:
                            opcode = MTOpcode.bastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.castore:
                            opcode = MTOpcode.castore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.sastore:
                            opcode = MTOpcode.sastore;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.pop:
                            opcode = MTOpcode.pop;
                            PrimitiveType[] expected =
                            {
                                PrimitiveType.Int, PrimitiveType.Float, PrimitiveType.Reference,
                                PrimitiveType.SubroutinePointer
                            };
                            emulatedStack.PopWithAssert(expected);
                            SetNextStack();
                            break;
                        case JavaOpcode.pop2:
                        {
                            var t = emulatedStack.Pop();
                            if ((t & PrimitiveType.IsDouble) != 0)
                            {
                                opcode = MTOpcode.pop;
                            }
                            else
                            {
                                emulatedStack.PopWithAssertIs32();
                                opcode = MTOpcode.pop2;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup:
                        {
                            opcode = MTOpcode.dup;
                            var t = emulatedStack.PopWithAssertIs32();
                            emulatedStack.Push(t);
                            emulatedStack.Push(t);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup_x1:
                        {
                            opcode = MTOpcode.dup_x1;
                            var t1 = emulatedStack.PopWithAssertIs32();
                            var t2 = emulatedStack.PopWithAssertIs32();
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup_x2:
                        {
                            data = null!;
                            var t1 = emulatedStack.PopWithAssertIs32();
                            var t2 = emulatedStack.Pop();
                            if ((t2 & PrimitiveType.IsDouble) != 0)
                            {
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x1;
                            }
                            else
                            {
                                var t3 = emulatedStack.PopWithAssertIs32();
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t3);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x2;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup2:
                        {
                            data = null!;
                            var t1 = emulatedStack.Pop();
                            if ((t1 & PrimitiveType.IsDouble) != 0)
                            {
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup;
                            }
                            else
                            {
                                var t2 = emulatedStack.PopWithAssertIs32();
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup2;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup2_x1:
                        {
                            data = null!;
                            var t1 = emulatedStack.Pop();

                            if ((t1 & PrimitiveType.IsDouble) != 0)
                            {
                                var t2 = emulatedStack.PopWithAssertIs32();
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup_x1;
                            }
                            else
                            {
                                var t2 = emulatedStack.PopWithAssertIs32();
                                var t3 = emulatedStack.PopWithAssertIs32();
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                emulatedStack.Push(t3);
                                emulatedStack.Push(t2);
                                emulatedStack.Push(t1);
                                opcode = MTOpcode.dup2_x1;
                                intData = 0;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.dup2_x2:
                            throw new NotImplementedException("No dup2_x2 opcode");
                        case JavaOpcode.swap:
                        {
                            opcode = MTOpcode.swap;
                            var t1 = emulatedStack.PopWithAssertIs32();
                            var t2 = emulatedStack.PopWithAssertIs32();
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t2);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.iadd:
                            opcode = MTOpcode.iadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.ladd:
                            opcode = MTOpcode.ladd;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fadd:
                            opcode = MTOpcode.fadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dadd:
                            opcode = MTOpcode.dadd;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.isub:
                            opcode = MTOpcode.isub;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lsub:
                            opcode = MTOpcode.lsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fsub:
                            opcode = MTOpcode.fsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dsub:
                            opcode = MTOpcode.dsub;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.imul:
                            opcode = MTOpcode.imul;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lmul:
                            opcode = MTOpcode.lmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fmul:
                            opcode = MTOpcode.fmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dmul:
                            opcode = MTOpcode.dmul;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.idiv:
                            opcode = MTOpcode.idiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.ldiv:
                            opcode = MTOpcode.ldiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fdiv:
                            opcode = MTOpcode.fdiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.ddiv:
                            opcode = MTOpcode.ddiv;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.irem:
                            opcode = MTOpcode.irem;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lrem:
                            opcode = MTOpcode.lrem;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.frem:
                            opcode = MTOpcode.frem;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.drem:
                            opcode = MTOpcode.drem;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.ineg:
                            opcode = MTOpcode.ineg;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lneg:
                            opcode = MTOpcode.lneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.fneg:
                            opcode = MTOpcode.fneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.dneg:
                            opcode = MTOpcode.dneg;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.ishl:
                            opcode = MTOpcode.ishl;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lshl:
                            opcode = MTOpcode.lshl;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.ishr:
                            opcode = MTOpcode.ishr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lshr:
                            opcode = MTOpcode.lshr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.iushr:
                            opcode = MTOpcode.iushr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lushr:
                            opcode = MTOpcode.lushr;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.iand:
                            opcode = MTOpcode.iand;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.land:
                            opcode = MTOpcode.land;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.ior:
                            opcode = MTOpcode.ior;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lor:
                            opcode = MTOpcode.lor;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.ixor:
                            opcode = MTOpcode.ixor;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lxor:
                            opcode = MTOpcode.lxor;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.iinc:
                            // sbyte cast here MUST BE because single-byte iinc may contain negative value, i.e. i-=1 is IINC 0x01 0xFF
                            intData = (sbyte)args[1];
                            shortData = args[0];
                            opcode = MTOpcode.iinc;
                            // no changes on stack
                            SetNextStack();
                            break;
                        case JavaOpcode.i2l:
                            opcode = MTOpcode.i2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.i2f:
                            opcode = MTOpcode.i2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.i2d:
                            opcode = MTOpcode.i2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.l2i:
                            opcode = MTOpcode.l2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.l2f:
                            opcode = MTOpcode.l2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.l2d:
                            opcode = MTOpcode.l2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.f2i:
                            opcode = MTOpcode.f2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.f2l:
                            opcode = MTOpcode.f2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.f2d:
                            opcode = MTOpcode.f2d;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Double);
                            SetNextStack();
                            break;
                        case JavaOpcode.d2i:
                            opcode = MTOpcode.d2i;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.d2l:
                            opcode = MTOpcode.d2l;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Long);
                            SetNextStack();
                            break;
                        case JavaOpcode.d2f:
                            opcode = MTOpcode.d2f;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Float);
                            SetNextStack();
                            break;
                        case JavaOpcode.i2b:
                            opcode = MTOpcode.i2b;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.i2c:
                            opcode = MTOpcode.i2c;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.i2s:
                            opcode = MTOpcode.i2s;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.lcmp:
                            opcode = MTOpcode.lcmp;
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.fcmpl:
                            opcode = MTOpcode.fcmpl;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.fcmpg:
                            opcode = MTOpcode.fcmpg;
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.dcmpl:
                            opcode = MTOpcode.dcmpl;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Int);
                            break;
                        case JavaOpcode.dcmpg:
                            opcode = MTOpcode.dcmpg;
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            emulatedStack.Push(PrimitiveType.Int);
                            break;
                        case JavaOpcode.ifeq:
                        {
                            opcode = MTOpcode.ifeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifne:
                        {
                            opcode = MTOpcode.ifne;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.iflt:
                        {
                            opcode = MTOpcode.iflt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifge:
                        {
                            opcode = MTOpcode.ifge;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifgt:
                        {
                            opcode = MTOpcode.ifgt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifle:
                        {
                            opcode = MTOpcode.ifle;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpeq:
                        {
                            opcode = MTOpcode.if_cmpeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpne:
                        {
                            opcode = MTOpcode.if_cmpne;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmplt:
                        {
                            opcode = MTOpcode.if_cmplt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpge:
                        {
                            opcode = MTOpcode.if_cmpge;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmpgt:
                        {
                            opcode = MTOpcode.if_cmpgt;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_icmple:
                        {
                            opcode = MTOpcode.if_cmple;
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_acmpeq:
                        {
                            opcode = MTOpcode.if_cmpeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.if_acmpne:
                        {
                            opcode = MTOpcode.if_cmpne;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            int target = CalcTargetInstruction();
                            intData = target;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.@goto:
                        {
                            int target = CalcTargetInstruction();
                            SetStack(target);
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
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
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
                            output[instrIndex] = new LinkedInstruction(MTOpcode.tableswitch, shortData, intData, data);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.lookupswitch:
                        {
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
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
                            output[instrIndex] = new LinkedInstruction(MTOpcode.lookupswitch, shortData, intData, data);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        }
                        case JavaOpcode.ireturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.lreturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Long);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.freturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Float);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.dreturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Double);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.areturn:
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.@return:
                        {
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
                                var index = jvm.StaticFieldsOwners.IndexOf(f);
                                if (index < 0)
                                {
                                    // maybe it's a native field?
                                    if (f.GetValue == null)
                                    {
                                        // no, it's not.
                                        throw new JavaLinkageException($"Static field {d} has no static slot!");
                                    }

                                    opcode = MTOpcode.bridge_init_class;
                                    intData = 1;
                                    data = new ClassBoundBridge(f.GetValue, c);
                                }
                                else
                                {
                                    intData = index;
                                    data = c;
                                    opcode = MTOpcode.get_static;
                                }
                            }

                            if (d != default)
                                emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.putstatic:
                        {
                            emulatedStack.Pop(); // TODO check field type
                            var f = getFieldSafely(cls, args, true, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                // set to "native" static is forbidden in MIDP/CLDC specs. we do not check this case.
                                var index = jvm.StaticFieldsOwners.IndexOf(f);
                                if (index < 0)
                                    throw new JavaLinkageException($"Static field {d} has no static slot!");

                                intData = index;
                                data = c;
                                opcode = MTOpcode.set_static;
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.getfield:
                        {
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            var f = getFieldSafely(cls, args, false, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                if (jvm.UseBridgesForFields)
                                {
                                    var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                                    opcode = MTOpcode.bridge_init_class;
                                    intData = 1;
                                    data = new ClassBoundBridge(b, c);
                                }
                                else
                                {
                                    opcode = MTOpcode.get_field;
                                    data = new ReflectionFieldPointer(f.NativeField!, c);
                                }
                            }

                            if (d != default)
                                emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.putfield:
                        {
                            emulatedStack.Pop(); // TODO check field type
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            var f = getFieldSafely(cls, args, false, ref opcode, ref data, out var d, out var c);
                            if (f != null)
                            {
                                if (jvm.UseBridgesForFields)
                                {
                                    var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                                    opcode = MTOpcode.bridge_init_class;
                                    intData = 2;
                                    data = new ClassBoundBridge(b, c);
                                }
                                else
                                {
                                    opcode = MTOpcode.set_field;
                                    data = new ReflectionFieldPointer(f.NativeField!, c);
                                }
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.invokevirtual:
                        case JavaOpcode.invokeinterface:
                        {
                            var vp = LinkVirtualCall(jvm, consts, args);
                            intData = vp.Item1;
                            shortData = vp.Item2;
                            opcode = MTOpcode.invoke_virtual;
                            var d = DescriptorUtils.ParseMethodDescriptorAsPrimitives(jvm.DecodeVirtualPointer(vp.Item1)
                                .Descriptor);
                            foreach (var p in d.args.Reverse()) emulatedStack.PopWithAssert(p);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            if (d.returnType.HasValue) emulatedStack.Push(d.returnType.Value);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.invokespecial:
                        case JavaOpcode.invokestatic:
                        {
                            var ndc = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                            var @class = jvm.Classes[ndc.ClassName];
                            var m = @class.GetMethodRecursive(ndc.Descriptor);
                            data = m;
                            var d = DescriptorUtils.ParseMethodDescriptorAsPrimitives(ndc.Descriptor.Descriptor);
                            foreach (var p in d.args.Reverse()) emulatedStack.PopWithAssert(p);
                            if (instruction.Opcode == JavaOpcode.invokespecial)
                            {
                                emulatedStack.PopWithAssert(PrimitiveType.Reference);
                                opcode = MTOpcode.invoke_instance;
                            }
                            else
                            {
                                opcode = MTOpcode.invoke_static;
                            }

                            if (d.returnType.HasValue)
                                emulatedStack.Push(d.returnType.Value);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.invokedynamic:
                            throw new NotImplementedException("No invokedynamic support");
                        case JavaOpcode.newobject:
                        {
                            opcode = MTOpcode.new_obj;
                            var type = (string)consts[Combine(args[0], args[1])];
                            if (!jvm.Classes.TryGetValue(type, out var cls1))
                                throw new JavaLinkageException($"Class \"{type}\" is not registered");
                            data = cls1;
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.newarray:
                            opcode = MTOpcode.new_prim_arr;
                            intData = (int)(ArrayType)args[0];
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
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
                            data = jvm.GetClass(arrType);
                            emulatedStack.PopWithAssert(PrimitiveType.Int);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.arraylength:
                            opcode = MTOpcode.array_length;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        case JavaOpcode.athrow:
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            output[instrIndex] = new LinkedInstruction(MTOpcode.athrow);
                            isLinked[instrIndex] = true;
                            goto entryPointsLoop;
                        case JavaOpcode.checkcast:
                        {
                            opcode = MTOpcode.checkcast;
                            var type = (string)consts[Combine(args[0], args[1])];
                            data = jvm.GetClass(type);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.instanceof:
                        {
                            opcode = MTOpcode.instanceof;
                            var type = (string)consts[Combine(args[0], args[1])];
                            data = jvm.GetClass(type);
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            emulatedStack.Push(PrimitiveType.Int);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.monitorenter:
                            opcode = MTOpcode.monitor_enter;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        case JavaOpcode.monitorexit:
                            opcode = MTOpcode.monitor_exit;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            SetNextStack();
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
                                        emulatedStack.PopWithAssert(PrimitiveType.Reference);
                                        break;
                                    case JavaOpcode.istore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Int);
                                        break;
                                    case JavaOpcode.lstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Long);
                                        break;
                                    case JavaOpcode.fstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Float);
                                        break;
                                    case JavaOpcode.dstore:
                                        opcode = MTOpcode.store;
                                        emulatedStack.PopWithAssert(PrimitiveType.Double);
                                        break;
                                    default:
                                        throw new JavaRuntimeError($"Invalid wide opcode {op}");
                                }
                            }

                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.multianewarray:
                        {
                            opcode = MTOpcode.new_multi_arr;
                            var dims = args[2];
                            var type = (string)consts[Combine(args[0], args[1])];
                            intData = dims;
                            data = jvm.GetClass(type);
                            for (int i = 0; i < dims; i++)
                            {
                                if (type[i] != '[')
                                    throw new JavaLinkageException(
                                        $"Multiarray has invalid type: \"{type}\" for {dims} dimensions");
                                emulatedStack.PopWithAssert(PrimitiveType.Int);
                            }

                            emulatedStack.Push(PrimitiveType.Reference);
                            SetNextStack();
                            break;
                        }
                        case JavaOpcode.ifnull:
                        {
                            opcode = MTOpcode.ifeq;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            int target = CalcTargetInstruction();
                            intData = target;
                            data = null!;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.ifnonnull:
                        {
                            opcode = MTOpcode.ifne;
                            emulatedStack.PopWithAssert(PrimitiveType.Reference);
                            int target = CalcTargetInstruction();
                            intData = target;
                            data = null!;
                            SetNextStack();
                            SetStack(target);
                            entryPoints.Push(target);
                            break;
                        }
                        case JavaOpcode.goto_w:
                        case JavaOpcode.jsr_w:
                            throw new NotImplementedException("No wide jumps!");
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
                        throw new JavaLinkageException(
                            $"There is no opcode at offset {globalOffset} (relative {relativeOffset}).\nAvailable offsets:\n{offsetsPrint}");
                    }

                    void SetNextStack() => SetStack(instrIndex + 1);
                }
            }
        }
        catch (JavaLinkageException)
        {
            throw;
        }
        catch
        {
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = new LinkedInstruction(MTOpcode.error_bytecode);
            }

            stackBeforeInstruction = null!;
        }

        method.LinkedCatches = LinkCatches(method.Catches, consts, offsets, jvm);
        method.LinkedCode = output;
        method.StackTypes = stackBeforeInstruction!;
    }

    private static (int, ushort) LinkVirtualCall(JvmState jvm, object[] consts, byte[] args)
    {
        var ndc = consts[Combine(args[0], args[1])];
        NameDescriptor nd;
        switch (ndc)
        {
            case NameDescriptor r:
                nd = r;
                break;
            case NameDescriptorClass c:
                nd = c.Descriptor;
                break;
            default:
                throw new JavaLinkageException(
                    $"Argument for virtual call was not a descriptor object but {ndc.GetType()}");
        }

        var argsCount = DescriptorUtils.ParseMethodArgsCount(nd.Descriptor);
        return (jvm.GetVirtualPointer(nd), (ushort)argsCount);
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

        if (!jvm.Classes.TryGetValue(ndc.ClassName, out c!))
        {
            var msg = $"\"{ndc.ClassName}\" can't be found but its field \"{ndc.Descriptor}\" is going to be used";
            logger?.Log(LoadIssueType.MissingClassAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_class;
            data = ndc.ClassName;
            return null;
        }

        var f = c.GetFieldRecursiveOrNull(ndc.Descriptor);

        if (f == null)
        {
            var msg = $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"";
            logger?.Log(LoadIssueType.MissingFieldAccess, cls.Name, msg);
            opcode = MTOpcode.error_no_field;
            data = ndc.Descriptor.Name;
            return null;
        }

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

    public static int Combine(byte indexByte1, byte indexByte2)
    {
        var u = (ushort)((indexByte1 << 8) | indexByte2);
        var s = (short)u;
        return s;
    }

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

    public class EmulatedFrameStack
    {
        private readonly Stack<PrimitiveType> _stack = new();

        private readonly int _maxLength;
        private readonly Instruction[] _code;

        public int InstrIndex;

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

        public PrimitiveType Pop()
        {
            if (_stack.Count == 0)
                throw new StackMismatchException(
                    $"{Current()} attempts to pop from empty stack");
            return _stack.Pop();
        }

        public PrimitiveType PopWithAssertIs32()
        {
            var real = Pop();
            if ((real & PrimitiveType.IsDouble) != 0)
                throw new StackMismatchException(
                    $"{Current()} expects 32-bit value on stack but got {real}");
            return real;
        }

        public void PopWithAssert(PrimitiveType expected)
        {
            var real = Pop();
            if (expected != real)
                throw new StackMismatchException($"{Current()} expects {expected} on stack but got {real}");
        }

        public void PopWithAssert(params PrimitiveType[] expected)
        {
            var real = Pop();
            if (!expected.Contains(real))
            {
                var expectedStr = expected.Length == 1
                    ? $"expects {expected[0]}"
                    : $"expects one of {string.Join(", ", expected)}";
                throw new StackMismatchException($"{Current()} {expectedStr} on stack but got {real}");
            }
        }

        private string Current() => $"Opcode {_code[InstrIndex].Opcode} at {InstrIndex}";

        public void Clear() => _stack.Clear();

        public PrimitiveType[] ToArray() => _stack.ToArray();
    }
}