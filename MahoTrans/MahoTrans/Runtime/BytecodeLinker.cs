// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public static class BytecodeLinker
{
    public static void Link(JavaMethodBody method, JvmState jvm)
    {
        var isClinit = method.Method.Descriptor == new NameDescriptor("<clinit>", "()V");
        var cls = method.Method.Class;
#if DEBUG
        LinkInternal(method, cls, jvm, isClinit);
#else
        try
        {
            LinkInternal(method, cls, jvm, isClinit);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException(
                $"Failed to link {method} in JIT manner", e);
        }
#endif
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

            Instruction[] code;

            try
            {
                code = method.JavaBody.Code;
            }
            catch
            {
                continue;
            }

            VerifyClassReferences(code, cls, jvm, logger);
            VerifyLocals(method.JavaBody, cls.Name, logger);
            CheckMethodExit(code, method.Descriptor.ToString(), cls.Name, logger);
        }
    }

    /// <summary>
    ///     Checks that there is no broken references.
    /// </summary>
    private static void VerifyClassReferences(Instruction[] code, JavaClass cls, JvmState jvm, ILoadTimeLogger logger)
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
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
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
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
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
                        logger.Log(LoadIssueType.InvalidConstant, cls.Name,
                            $"Constant \"{Combine(args[0], args[1])}\" isn't a member reference");
                        break;
                    }

                    if (jvm.Classes.TryGetValue(ndc.ClassName, out var c))
                    {
                        if (c.IsInterface)
                            break;
                        try
                        {
                            c.GetMethodRecursive(ndc.Descriptor);
                        }
                        catch
                        {
                            logger.Log(LoadIssueType.MissingMethodAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no method \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
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
                        logger.Log(LoadIssueType.InvalidConstant, cls.Name,
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
                                logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                    $"\"{ndc.ClassName}\" has field \"{ndc.Descriptor}\", but it is static");
                            }
                        }
                        catch
                        {
                            logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
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
                        logger.Log(LoadIssueType.InvalidConstant, cls.Name,
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
                                logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                    $"\"{ndc.ClassName}\" has field \"{ndc.Descriptor}\", but it is not static");
                            }
                        }
                        catch
                        {
                            logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field \"{ndc.Descriptor}\"");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its field \"{ndc.Descriptor}\" will be used");
                    }

                    break;
                }
            }
        }
    }


    private static void VerifyLocals(JavaMethodBody method, string cls, ILoadTimeLogger logger)
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
                    logger.Log(LoadIssueType.LocalVariableIndexOutOfBounds, cls,
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
                logger.Log(LoadIssueType.MultiTypeLocalVariable, cls,
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

    private static void CheckMethodExit(Instruction[] code, string method, string cls, ILoadTimeLogger logger)
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

        logger.Log(LoadIssueType.MethodWithoutReturn, cls,
            $"{method}'s last instruction is {lastOpcode}, which does not terminate the method.");
    }

    private static void LinkInternal(JavaMethodBody method, JavaClass cls, JvmState jvm, bool isClinit)
    {
        var code = method.Code;
        var consts = cls.Constants;
        var output = new LinkedInstruction[code.Length];
        var isLinked = new bool[code.Length];
        PrimitiveType[]?[] stackBeforeInstruction = new PrimitiveType[]?[code.Length];
        stackBeforeInstruction[0] = Array.Empty<PrimitiveType>(); // we enter with empty stack

        // offsets cache
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        for (int i = 0; i < code.Length; i++)
            offsets[code[i].Offset] = i;

        // stack verify data
        Stack<PrimitiveType> emulatedStack = new();

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
                throw new JavaLinkageException($"Stack mismatch at instruction {target}");
        }

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
                MTOpcode opcode;

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
                            throw new ArgumentException($"ldc was {obj.GetType()}");
                        }

                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.ldc2_w:
                    {
                        var obj = consts[Combine(args[0], args[1])];
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
                            else if (d == 1d)
                                opcode = MTOpcode.dconst_1;
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
                            throw new ArgumentException($"ldc2 was {obj.GetType()}");
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.laload:
                        opcode = MTOpcode.laload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.faload:
                        opcode = MTOpcode.faload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.daload:
                        opcode = MTOpcode.daload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.aaload:
                        opcode = MTOpcode.aaload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.baload:
                        opcode = MTOpcode.baload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.caload:
                        opcode = MTOpcode.caload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.saload:
                        opcode = MTOpcode.saload;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore:
                        opcode = MTOpcode.store;
                        intData = args[0];
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore:
                        opcode = MTOpcode.store;
                        intData = args[0];
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore:
                        opcode = MTOpcode.store;
                        intData = args[0];
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore:
                        opcode = MTOpcode.store;
                        intData = args[0];
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore:
                        opcode = MTOpcode.store;
                        intData = args[0];
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore_0:
                        opcode = MTOpcode.store_0;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore_1:
                        opcode = MTOpcode.store_1;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore_2:
                        opcode = MTOpcode.store_2;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore_3:
                        opcode = MTOpcode.store_3;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore_0:
                        opcode = MTOpcode.store_0;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore_1:
                        opcode = MTOpcode.store_1;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore_2:
                        opcode = MTOpcode.store_2;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore_3:
                        opcode = MTOpcode.store_3;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore_0:
                        opcode = MTOpcode.store_0;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore_1:
                        opcode = MTOpcode.store_1;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore_2:
                        opcode = MTOpcode.store_2;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore_3:
                        opcode = MTOpcode.store_3;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore_0:
                        opcode = MTOpcode.store_0;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore_1:
                        opcode = MTOpcode.store_1;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore_2:
                        opcode = MTOpcode.store_2;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore_3:
                        opcode = MTOpcode.store_3;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore_0:
                        opcode = MTOpcode.store_0;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore_1:
                        opcode = MTOpcode.store_1;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore_2:
                        opcode = MTOpcode.store_2;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore_3:
                        opcode = MTOpcode.store_3;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.iastore:
                        opcode = MTOpcode.iastore;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.lastore:
                        opcode = MTOpcode.lastore;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.fastore:
                        opcode = MTOpcode.fastore;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.dastore:
                        opcode = MTOpcode.dastore;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.aastore:
                        opcode = MTOpcode.aastore;
                        PopWithAssert(PrimitiveType.Reference);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.bastore:
                        opcode = MTOpcode.bastore;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.castore:
                        opcode = MTOpcode.castore;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.sastore:
                        opcode = MTOpcode.sastore;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.pop:
                        opcode = MTOpcode.pop;
                        PopWithAssert(PrimitiveType.Int, PrimitiveType.Float, PrimitiveType.Reference,
                            PrimitiveType.SubroutinePointer);
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
                            PopWithAssertIs32();
                            opcode = MTOpcode.pop2;
                        }

                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup:
                    {
                        opcode = MTOpcode.dup;
                        var t = PopWithAssertIs32();
                        emulatedStack.Push(t);
                        emulatedStack.Push(t);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup_x1:
                    {
                        opcode = MTOpcode.dup_x1;
                        var t1 = PopWithAssertIs32();
                        var t2 = PopWithAssertIs32();
                        emulatedStack.Push(t1);
                        emulatedStack.Push(t2);
                        emulatedStack.Push(t1);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup_x2:
                    {
                        data = null!;
                        var t1 = PopWithAssertIs32();
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
                            var t3 = PopWithAssertIs32();
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
                            var t2 = PopWithAssertIs32();
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
                        var t1 = PopWithAssertIs32();
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
                            var t3 = PopWithAssertIs32();
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
                        var t1 = PopWithAssertIs32();
                        var t2 = PopWithAssertIs32();
                        emulatedStack.Push(t1);
                        emulatedStack.Push(t2);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.iadd:
                        opcode = MTOpcode.iadd;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.ladd:
                        opcode = MTOpcode.ladd;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fadd:
                        opcode = MTOpcode.fadd;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dadd:
                        opcode = MTOpcode.dadd;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.isub:
                        opcode = MTOpcode.isub;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lsub:
                        opcode = MTOpcode.lsub;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fsub:
                        opcode = MTOpcode.fsub;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dsub:
                        opcode = MTOpcode.dsub;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.imul:
                        opcode = MTOpcode.imul;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lmul:
                        opcode = MTOpcode.lmul;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fmul:
                        opcode = MTOpcode.fmul;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dmul:
                        opcode = MTOpcode.dmul;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.idiv:
                        opcode = MTOpcode.idiv;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.ldiv:
                        opcode = MTOpcode.ldiv;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fdiv:
                        opcode = MTOpcode.fdiv;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.ddiv:
                        opcode = MTOpcode.ddiv;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.irem:
                        opcode = MTOpcode.irem;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lrem:
                        opcode = MTOpcode.lrem;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.frem:
                        opcode = MTOpcode.frem;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.drem:
                        opcode = MTOpcode.drem;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.ineg:
                        opcode = MTOpcode.ineg;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lneg:
                        opcode = MTOpcode.lneg;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fneg:
                        opcode = MTOpcode.fneg;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dneg:
                        opcode = MTOpcode.dneg;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.ishl:
                        opcode = MTOpcode.ishl;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lshl:
                        opcode = MTOpcode.lshl;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ishr:
                        opcode = MTOpcode.ishr;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lshr:
                        opcode = MTOpcode.lshr;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iushr:
                        opcode = MTOpcode.iushr;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lushr:
                        opcode = MTOpcode.lushr;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iand:
                        opcode = MTOpcode.iand;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.land:
                        opcode = MTOpcode.land;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ior:
                        opcode = MTOpcode.ior;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lor:
                        opcode = MTOpcode.lor;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ixor:
                        opcode = MTOpcode.ixor;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lxor:
                        opcode = MTOpcode.lxor;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iinc:
                        intData = args[1];
                        shortData = args[0];
                        opcode = MTOpcode.iinc;
                        // no changes on stack
                        SetNextStack();
                        break;
                    case JavaOpcode.i2l:
                        opcode = MTOpcode.i2l;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2f:
                        opcode = MTOpcode.i2f;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2d:
                        opcode = MTOpcode.i2d;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2i:
                        opcode = MTOpcode.l2i;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2f:
                        opcode = MTOpcode.l2f;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2d:
                        opcode = MTOpcode.l2d;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2i:
                        opcode = MTOpcode.f2i;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2l:
                        opcode = MTOpcode.f2l;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2d:
                        opcode = MTOpcode.f2d;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2i:
                        opcode = MTOpcode.d2i;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2l:
                        opcode = MTOpcode.d2l;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2f:
                        opcode = MTOpcode.d2f;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2b:
                        opcode = MTOpcode.i2b;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2c:
                        opcode = MTOpcode.i2c;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2s:
                        opcode = MTOpcode.i2s;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lcmp:
                        opcode = MTOpcode.lcmp;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.fcmpl:
                        opcode = MTOpcode.fcmpl;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.fcmpg:
                        opcode = MTOpcode.fcmpg;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.dcmpl:
                        opcode = MTOpcode.dcmpl;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Int);
                        break;
                    case JavaOpcode.dcmpg:
                        opcode = MTOpcode.dcmpg;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Int);
                        break;
                    case JavaOpcode.ifeq:
                    {
                        opcode = MTOpcode.ifeq;
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Reference);
                        PopWithAssert(PrimitiveType.Reference);
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
                        PopWithAssert(PrimitiveType.Reference);
                        PopWithAssert(PrimitiveType.Reference);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.lreturn:
                        PopWithAssert(PrimitiveType.Long);
                        output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.freturn:
                        PopWithAssert(PrimitiveType.Float);
                        output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.dreturn:
                        PopWithAssert(PrimitiveType.Double);
                        output[instrIndex] = new LinkedInstruction(MTOpcode.return_value);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.areturn:
                        PopWithAssert(PrimitiveType.Reference);
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
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
                        if (jvm.UseBridgesForFields)
                        {
                            var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                            opcode = MTOpcode.bridge_init_class;
                            intData = 0;
                            data = new ClassBoundBridge(b, c);
                        }
                        else
                        {
                            opcode = MTOpcode.get_field;
                            data = new ReflectionFieldPointer(f.NativeField, c);
                        }

                        emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.putstatic:
                    {
                        emulatedStack.Pop(); // TODO check field type
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
                        if (jvm.UseBridgesForFields)
                        {
                            var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                            opcode = MTOpcode.bridge_init_class;
                            intData = 1;
                            data = new ClassBoundBridge(b, c);
                        }
                        else
                        {
                            opcode = MTOpcode.set_field;
                            data = new ReflectionFieldPointer(f.NativeField, c);
                        }

                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.getfield:
                    {
                        PopWithAssert(PrimitiveType.Reference);
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
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
                            data = new ReflectionFieldPointer(f.NativeField, c);
                        }

                        emulatedStack.Push(DescriptorUtils.ParseDescriptor(d.Descriptor.Descriptor[0]));
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.putfield:
                    {
                        emulatedStack.Pop(); // TODO check field type
                        PopWithAssert(PrimitiveType.Reference);
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
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
                            data = new ReflectionFieldPointer(f.NativeField, c);
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
                        foreach (var p in d.args.Reverse()) PopWithAssert(p);
                        PopWithAssert(PrimitiveType.Reference);
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
                        foreach (var p in d.args.Reverse()) PopWithAssert(p);
                        if (instruction.Opcode == JavaOpcode.invokespecial)
                        {
                            PopWithAssert(PrimitiveType.Reference);
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
                        PopWithAssert(PrimitiveType.Int);
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
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.arraylength:
                        opcode = MTOpcode.array_length;
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.athrow:
                        PopWithAssert(PrimitiveType.Reference);
                        output[instrIndex] = new LinkedInstruction(MTOpcode.athrow);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.checkcast:
                    {
                        opcode = MTOpcode.checkcast;
                        var type = (string)consts[Combine(args[0], args[1])];
                        data = jvm.GetClass(type);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.instanceof:
                    {
                        opcode = MTOpcode.instanceof;
                        var type = (string)consts[Combine(args[0], args[1])];
                        data = jvm.GetClass(type);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.monitorenter:
                        opcode = MTOpcode.monitor_enter;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.monitorexit:
                        opcode = MTOpcode.monitor_exit;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.wide:
                    {
                        var op = (JavaOpcode)args[0];
                        if (op == JavaOpcode.iinc)
                        {
                            opcode = MTOpcode.iinc;
                            shortData = (ushort)Combine(args[1], args[2]);
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
                                    PopWithAssert(PrimitiveType.Reference);
                                    break;
                                case JavaOpcode.istore:
                                    opcode = MTOpcode.store;
                                    PopWithAssert(PrimitiveType.Int);
                                    break;
                                case JavaOpcode.lstore:
                                    opcode = MTOpcode.store;
                                    PopWithAssert(PrimitiveType.Long);
                                    break;
                                case JavaOpcode.fstore:
                                    opcode = MTOpcode.store;
                                    PopWithAssert(PrimitiveType.Float);
                                    break;
                                case JavaOpcode.dstore:
                                    opcode = MTOpcode.store;
                                    PopWithAssert(PrimitiveType.Double);
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
                            PopWithAssert(PrimitiveType.Int);
                        }

                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.ifnull:
                    {
                        opcode = MTOpcode.ifeq;
                        PopWithAssert(PrimitiveType.Reference);
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
                        PopWithAssert(PrimitiveType.Reference);
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

                void PopWithAssert(params PrimitiveType[] expected)
                {
                    var real = emulatedStack.Pop();
                    if (!expected.Contains(real))
                        throw new JavaLinkageException(
                            $"Opcode {instruction.Opcode} at {instrIndex} expects one of {string.Join(", ", expected)} on stack but got {real}");
                }

                PrimitiveType PopWithAssertIs32()
                {
                    var real = emulatedStack.Pop();
                    if ((real & PrimitiveType.IsDouble) != 0)
                        throw new JavaLinkageException(
                            $"Opcode {instruction.Opcode} at {instrIndex} expects 32-bit value on stack but got {real}");
                    return real;
                }
            }
        }

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
}