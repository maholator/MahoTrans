using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public static class BytecodeLinker
{
    public static LinkedInstruction[] Link(JavaMethodBody method, JvmState jvm)
    {
        var isClinit = method.Method.Descriptor == new NameDescriptor("<clinit>", "()V");
        var cls = method.Method.Class;
#if DEBUG
        return LinkInternal(method, cls, jvm, isClinit);
#else
        try
        {
            return LinkInternal(method, cls, jvm, isClinit);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException(
                $"Failed to link {method} in JIT manner", e);
        }
#endif
    }

    public static void VerifyBytecode(JavaClass cls, JvmState jvm)
    {
        var logger = jvm.Toolkit.LoadLogger;
        foreach (var method in cls.Methods.Values)
        {
            if (method.IsNative)
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
            CheckLocalsBounds(code, method.Descriptor.ToString(), method.JavaBody.LocalsCount, cls.Name, logger);
            CheckMethodExit(code, method.Descriptor.ToString(), cls.Name, logger);
        }
    }

    /// <summary>
    /// Checks that there is no broken references.
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


    private static void CheckLocalsBounds(Instruction[] code, string method, int localsCount, string cls,
        ILoadTimeLogger logger)
    {
        List<LocalType>[] locals = new List<LocalType>[localsCount];
        for (int i = 0; i < localsCount; i++)
            locals[i] = new List<LocalType>();

        for (int i = 0; i < code.Length; i++)
        {
            var opcode = code[i].Opcode.ToString();
            if (opcode.IndexOf("load", StringComparison.Ordinal) == 1 ||
                opcode.IndexOf("store", StringComparison.Ordinal) == 1)
            {
                char type = opcode[0];
                int index;
                if (opcode.IndexOf('_') != -1)
                {
                    index = int.Parse(opcode.Split('_')[1]);
                }
                else
                {
                    index = code[i].Args[0];
                }

                if (index >= localsCount)
                {
                    logger.Log(LoadIssueType.LocalVariableIndexOutOfBounds, cls,
                        $"Local variable {index} of type \"{(LocalType)type}\" is out of bounds at {method}:{i}");
                    continue;
                }

                if (!locals[index].Contains((LocalType)type))
                {
                    locals[index].Add((LocalType)type);
                }
            }
        }

        for (int i = 0; i < localsCount; i++)
        {
            if (locals[i].Count > 1)
            {
                locals[i].Sort();
                logger.Log(LoadIssueType.MultiTypeLocalVariable, cls,
                    $"Local variable {i} has multiple types: {string.Join(", ", locals[i])} at {method}");
            }
        }
    }

    public enum LocalType : ushort
    {
        Int = 'i',
        Long = 'l',
        Float = 'f',
        Double = 'd',
        Reference = 'a',
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

    private static LinkedInstruction[] LinkInternal(JavaMethodBody method, JavaClass cls, JvmState jvm, bool isClinit)
    {
        var code = method.Code;
        var consts = cls.Constants;
        var output = new LinkedInstruction[code.Length];
        var isLinked = new bool[code.Length];
        PrimitiveType[]?[] stackBeforeInsruction = new PrimitiveType[]?[code.Length];
        stackBeforeInsruction[0] = Array.Empty<PrimitiveType>(); // we enter with empty stack

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
            stackBeforeInsruction[offsets[methodCatch.CatchStart]] = new[] { PrimitiveType.Reference };
        }

        entryPoints.Push(0);

        void SetStack(int target)
        {
            var now = emulatedStack.ToArray();
            var was = stackBeforeInsruction[target];
            if (was == null)
                stackBeforeInsruction[target] = now;
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
            var stackOnEntry = stackBeforeInsruction[entryPoint];
            if (stackOnEntry == null)
                throw new JavaLinkageException($"Method can't be entered at {entryPoint}");
            foreach (var el in stackOnEntry.Reverse())
                emulatedStack.Push(el);

            for (int instrIndex = entryPoint; instrIndex < code.Length; instrIndex++)
            {
                var instruction = code[instrIndex];
                var args = instruction.Args;
                object data;
                int intData = 0;
                ushort shortData = 0;
                var opcode = instruction.Opcode;

                switch (instruction.Opcode)
                {
                    case JavaOpcode.nop:
                        data = null!;
                        SetNextStack();
                        break;
                    case JavaOpcode.aconst_null:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.iconst_m1:
                    case JavaOpcode.iconst_0:
                    case JavaOpcode.iconst_1:
                    case JavaOpcode.iconst_2:
                    case JavaOpcode.iconst_3:
                    case JavaOpcode.iconst_4:
                    case JavaOpcode.iconst_5:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lconst_0:
                    case JavaOpcode.lconst_1:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fconst_0:
                    case JavaOpcode.fconst_1:
                    case JavaOpcode.fconst_2:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dconst_0:
                    case JavaOpcode.dconst_1:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.bipush:
                    {
                        intData = unchecked((sbyte)args[0]);
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.sipush:
                        intData = Combine(args[0], args[1]);
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.ldc:
                        data = consts[args[0]];
                        PushUnknown(data);
                        SetNextStack();
                        break;
                    case JavaOpcode.ldc_w:
                    case JavaOpcode.ldc2_w:
                        data = consts[Combine(args[0], args[1])];
                        // this is a copy-paste from ldc
                        PushUnknown(data);
                        SetNextStack();
                        break;
                    case JavaOpcode.iload:
                        intData = args[0];
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lload:
                        intData = args[0];
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fload:
                        intData = args[0];
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dload:
                        intData = args[0];
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.aload:
                        intData = args[0];
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.iload_0:
                    case JavaOpcode.iload_1:
                    case JavaOpcode.iload_2:
                    case JavaOpcode.iload_3:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lload_0:
                    case JavaOpcode.lload_1:
                    case JavaOpcode.lload_2:
                    case JavaOpcode.lload_3:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fload_0:
                    case JavaOpcode.fload_1:
                    case JavaOpcode.fload_2:
                    case JavaOpcode.fload_3:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dload_0:
                    case JavaOpcode.dload_1:
                    case JavaOpcode.dload_2:
                    case JavaOpcode.dload_3:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.aload_0:
                    case JavaOpcode.aload_1:
                    case JavaOpcode.aload_2:
                    case JavaOpcode.aload_3:
                        data = null!;
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.iaload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.laload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.faload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.daload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.aaload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.baload:
                    case JavaOpcode.caload:
                    case JavaOpcode.saload:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore:
                        intData = args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore:
                        intData = args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore:
                        intData = args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore:
                        intData = args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore:
                        intData = args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.istore_0:
                    case JavaOpcode.istore_1:
                    case JavaOpcode.istore_2:
                    case JavaOpcode.istore_3:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lstore_0:
                    case JavaOpcode.lstore_1:
                    case JavaOpcode.lstore_2:
                    case JavaOpcode.lstore_3:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fstore_0:
                    case JavaOpcode.fstore_1:
                    case JavaOpcode.fstore_2:
                    case JavaOpcode.fstore_3:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dstore_0:
                    case JavaOpcode.dstore_1:
                    case JavaOpcode.dstore_2:
                    case JavaOpcode.dstore_3:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.astore_0:
                    case JavaOpcode.astore_1:
                    case JavaOpcode.astore_2:
                    case JavaOpcode.astore_3:
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.iastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.lastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.fastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.dastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.aastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.bastore:
                    case JavaOpcode.castore:
                    case JavaOpcode.sastore:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.pop:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int, PrimitiveType.Float, PrimitiveType.Reference,
                            PrimitiveType.SubroutinePointer);
                        SetNextStack();
                        break;
                    case JavaOpcode.pop2:
                    {
                        data = null!;
                        var t = emulatedStack.Pop();
                        if ((t & PrimitiveType.IsDouble) == 0)
                            PopWithAssertIs32();
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup:
                    {
                        data = null!;
                        var t = PopWithAssertIs32();
                        emulatedStack.Push(t);
                        emulatedStack.Push(t);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup_x1:
                    {
                        data = null!;
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
                        }
                        else
                        {
                            var t3 = PopWithAssertIs32();
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t3);
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
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
                        }
                        else
                        {
                            var t2 = PopWithAssertIs32();
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
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
                        }
                        else
                        {
                            var t3 = PopWithAssertIs32();
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
                            emulatedStack.Push(t3);
                            emulatedStack.Push(t2);
                            emulatedStack.Push(t1);
                        }

                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.dup2_x2:
                        throw new NotImplementedException("No dup2_x2 opcode");
                    case JavaOpcode.swap:
                    {
                        data = null!;
                        var t1 = PopWithAssertIs32();
                        var t2 = PopWithAssertIs32();
                        emulatedStack.Push(t1);
                        emulatedStack.Push(t2);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.iadd:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.ladd:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fadd:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dadd:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.isub:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lsub:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fsub:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dsub:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.imul:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lmul:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fmul:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dmul:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.idiv:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.ldiv:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fdiv:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.ddiv:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.irem:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lrem:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.frem:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.drem:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.ineg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lneg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.fneg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.dneg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.ishl:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lshl:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ishr:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lshr:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iushr:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lushr:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iand:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.land:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ior:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lor:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.ixor:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lxor:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.iinc:
                        intData = args[1];
                        shortData = args[0];
                        data = null!;
                        // no changes on stack
                        SetNextStack();
                        break;
                    case JavaOpcode.i2l:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2f:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2d:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2i:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2f:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.l2d:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2i:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2l:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.f2d:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Double);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2i:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2l:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Long);
                        SetNextStack();
                        break;
                    case JavaOpcode.d2f:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Float);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2b:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2c:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.i2s:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.lcmp:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        PopWithAssert(PrimitiveType.Long);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.fcmpl:
                    case JavaOpcode.fcmpg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        PopWithAssert(PrimitiveType.Float);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.dcmpl:
                    case JavaOpcode.dcmpg:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        PopWithAssert(PrimitiveType.Double);
                        emulatedStack.Push(PrimitiveType.Int);
                        break;
                    case JavaOpcode.ifeq:
                    case JavaOpcode.ifne:
                    case JavaOpcode.iflt:
                    case JavaOpcode.ifge:
                    case JavaOpcode.ifgt:
                    case JavaOpcode.ifle:
                    {
                        PopWithAssert(PrimitiveType.Int);
                        int target = CalcTargetInstruction();
                        intData = target;
                        data = null!;
                        SetNextStack();
                        SetStack(target);
                        entryPoints.Push(target);
                        break;
                    }
                    case JavaOpcode.if_icmpeq:
                    case JavaOpcode.if_icmpne:
                    case JavaOpcode.if_icmplt:
                    case JavaOpcode.if_icmpge:
                    case JavaOpcode.if_icmpgt:
                    case JavaOpcode.if_icmple:
                    {
                        PopWithAssert(PrimitiveType.Int);
                        PopWithAssert(PrimitiveType.Int);
                        int target = CalcTargetInstruction();
                        intData = target;
                        data = null!;
                        SetNextStack();
                        SetStack(target);
                        entryPoints.Push(target);
                        break;
                    }
                    case JavaOpcode.if_acmpeq:
                    case JavaOpcode.if_acmpne:
                    {
                        PopWithAssert(PrimitiveType.Reference);
                        PopWithAssert(PrimitiveType.Reference);
                        int target = CalcTargetInstruction();
                        intData = target;
                        data = null!;
                        SetNextStack();
                        SetStack(target);
                        entryPoints.Push(target);
                        break;
                    }
                    case JavaOpcode.@goto:
                    {
                        int target = CalcTargetInstruction();
                        intData = target;
                        data = null!;
                        SetStack(target);
                        entryPoints.Push(target);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
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
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
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
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    }
                    case JavaOpcode.ireturn:
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.lreturn:
                        data = null!;
                        PopWithAssert(PrimitiveType.Long);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.freturn:
                        data = null!;
                        PopWithAssert(PrimitiveType.Float);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.dreturn:
                        data = null!;
                        PopWithAssert(PrimitiveType.Double);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.areturn:
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.@return:
                    {
                        if (isClinit)
                            opcode = JavaOpcode._inplacereturn;
                        data = null!;
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    }
                    case JavaOpcode.getstatic:
                    {
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
                        var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                        data = new FieldPointer(b, c);
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
                        var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                        data = new FieldPointer(b, c);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.getfield:
                    {
                        PopWithAssert(PrimitiveType.Reference);
                        var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                        var c = jvm.Classes[d.ClassName];
                        var f = c.GetFieldRecursive(d.Descriptor);
                        var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                        data = new FieldPointer(b, c);
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
                        var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                        data = new FieldPointer(b, c);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.invokevirtual:
                    {
                        var vp = LinkVirtualCall(jvm, consts, args);
                        intData = vp.Item1;
                        shortData = vp.Item2;
                        data = null!;
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
                        if (opcode == JavaOpcode.invokespecial)
                            PopWithAssert(PrimitiveType.Reference);
                        if (d.returnType.HasValue) emulatedStack.Push(d.returnType.Value);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.invokeinterface:
                    {
                        var vp = LinkVirtualCall(jvm, consts, args);
                        intData = vp.Item1;
                        shortData = vp.Item2;
                        data = null!;
                        var d = DescriptorUtils.ParseMethodDescriptorAsPrimitives(jvm.DecodeVirtualPointer(vp.Item1)
                            .Descriptor);
                        foreach (var p in d.args.Reverse()) PopWithAssert(p);
                        PopWithAssert(PrimitiveType.Reference);
                        if (d.returnType.HasValue) emulatedStack.Push(d.returnType.Value);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.invokedynamic:
                        data = null!;
                        break;
                    case JavaOpcode.newobject:
                    {
                        var type = (string)consts[Combine(args[0], args[1])];
                        if (!jvm.Classes.TryGetValue(type, out var cls1))
                            throw new JavaLinkageException($"Class \"{type}\" is not registered");
                        data = cls1;
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.newarray:
                        intData = (int)(ArrayType)args[0];
                        data = null!;
                        PopWithAssert(PrimitiveType.Int);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.anewarray:
                    {
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
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    case JavaOpcode.athrow:
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                        isLinked[instrIndex] = true;
                        goto entryPointsLoop;
                    case JavaOpcode.checkcast:
                    {
                        var type = (string)consts[Combine(args[0], args[1])];
                        data = jvm.GetClass(type);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.instanceof:
                    {
                        var type = (string)consts[Combine(args[0], args[1])];
                        data = jvm.GetClass(type);
                        PopWithAssert(PrimitiveType.Reference);
                        emulatedStack.Push(PrimitiveType.Int);
                        SetNextStack();
                        break;
                    }
                    case JavaOpcode.monitorenter:
                    case JavaOpcode.monitorexit:
                        data = null!;
                        PopWithAssert(PrimitiveType.Reference);
                        SetNextStack();
                        break;
                    case JavaOpcode.wide:
                    {
                        data = args; // let's parse this in runtime
                        var op = (JavaOpcode)args[0];
                        if (op != JavaOpcode.iinc)
                        {
                            switch (op)
                            {
                                case JavaOpcode.aload:
                                    emulatedStack.Push(PrimitiveType.Reference);
                                    break;
                                case JavaOpcode.iload:
                                    emulatedStack.Push(PrimitiveType.Int);
                                    break;
                                case JavaOpcode.lload:
                                    emulatedStack.Push(PrimitiveType.Long);
                                    break;
                                case JavaOpcode.fload:
                                    emulatedStack.Push(PrimitiveType.Float);
                                    break;
                                case JavaOpcode.dload:
                                    emulatedStack.Push(PrimitiveType.Double);
                                    break;
                                case JavaOpcode.astore:
                                    PopWithAssert(PrimitiveType.Reference);
                                    break;
                                case JavaOpcode.istore:
                                    PopWithAssert(PrimitiveType.Int);
                                    break;
                                case JavaOpcode.lstore:
                                    PopWithAssert(PrimitiveType.Long);
                                    break;
                                case JavaOpcode.fstore:
                                    PopWithAssert(PrimitiveType.Float);
                                    break;
                                case JavaOpcode.dstore:
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
                        var dims = args[2];
                        var type = (string)consts[Combine(args[0], args[1])];
                        data = new MultiArrayInitializer(dims, jvm.GetClass(type));
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
                    case JavaOpcode.ifnonnull:
                    {
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
                    case JavaOpcode.breakpoint:
                        data = null!;
                        // TODO stack
                        break;
                    case JavaOpcode._inplacereturn:
                        throw new JavaLinkageException(
                            "inplacereturn opcode can be generated by linker but can't arrive in method body.");
                    case JavaOpcode._invokeany:
                        // we know nothing at link time.
                        data = null!;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                output[instrIndex] = new LinkedInstruction(opcode, shortData, intData, data);
                isLinked[instrIndex] = true;

                void PushUnknown(object o)
                {
                    if (o is int)
                        emulatedStack.Push(PrimitiveType.Int);
                    else if (o is long)
                        emulatedStack.Push(PrimitiveType.Long);
                    else if (o is float)
                        emulatedStack.Push(PrimitiveType.Float);
                    else if (o is double)
                        emulatedStack.Push(PrimitiveType.Double);
                    else if (o is string)
                        emulatedStack.Push(PrimitiveType.Reference);
                    else
                        throw new JavaLinkageException($"Unsupported constant type: {o.GetType()}");
                }

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

        return output;
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
}