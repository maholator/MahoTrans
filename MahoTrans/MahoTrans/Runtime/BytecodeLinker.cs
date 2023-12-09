using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public static class BytecodeLinker
{
    public static LinkedInstruction[] Link(JavaMethodBody method, JvmState jvm, Instruction[] code)
    {
        var isClinit = method.Method.Descriptor == new NameDescriptor("<clinit>", "()V");
        var cls = method.Method.Class;
#if DEBUG
        return LinkInternal(cls, jvm, code, isClinit);
#else
        try
        {
            return LinkInternal(cls, jvm, code, isClinit);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException(
                $"Failed to perform JIT linking for method {method} in class {method.Method?.Class}", e);
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
            CheckLocalsBounds(code, method.Descriptor.ToString(), method.JavaBody.LocalsCount, cls, logger);
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
                        logger.Log(LoadIssueType.BrokenConstant, cls.Name,
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
                                $"\"{ndc.ClassName}\" has no method {ndc.Descriptor}");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its method {ndc.Descriptor} will be used");
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
                        logger.Log(LoadIssueType.BrokenConstant, cls.Name,
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
                                    $"\"{ndc.ClassName}\" has field {ndc.Descriptor}, but it is static");
                            }
                        }
                        catch
                        {
                            logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field {ndc.Descriptor}");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its field {ndc.Descriptor} will be used");
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
                        logger.Log(LoadIssueType.BrokenConstant, cls.Name,
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
                                    $"\"{ndc.ClassName}\" has field {ndc.Descriptor}, but it is not static");
                            }
                        }
                        catch
                        {
                            logger.Log(LoadIssueType.MissingFieldAccess, cls.Name,
                                $"\"{ndc.ClassName}\" has no field {ndc.Descriptor}");
                        }
                    }
                    else
                    {
                        logger.Log(LoadIssueType.MissingClassAccess, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its field {ndc.Descriptor} will be used");
                    }

                    break;
                }
            }
        }
    }


    private static void CheckLocalsBounds(Instruction[] code, string methodName, int localsCount, JavaClass cls,
        ILoadTimeLogger logger)
    {
        List<char>[] locals = new List<char>[localsCount];
        for (int i = 0; i < localsCount; i++)
            locals[i] = new List<char>();

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
                    logger.Log(LoadIssueType.LocalVariableIndexOutOfBounds, cls.Name,
                        $"Local variable {index} of type \"{type}\" is out of bounds at {methodName}:{i}");
                }

                if (!locals[index].Contains(type))
                {
                    locals[index].Add(type);
                }
            }
        }

        for (int i = 0; i < localsCount; i++)
        {
            if (locals[i].Count > 1)
            {
                logger.Log(LoadIssueType.MultitypeLocalVariable, cls.Name,
                    $"Local variable {i} has multiple types: {string.Join(", ", locals[i])} at {methodName}");
            }
        }
    }

    private static LinkedInstruction[] LinkInternal(JavaClass cls, JvmState jvm, Instruction[] code, bool isClinit)
    {
        Dictionary<int, int> offsets = new Dictionary<int, int>();
        for (int i = 0; i < code.Length; i++)
            offsets[code[i].Offset] = i;

        var consts = cls.Constants;
        var output = new LinkedInstruction[code.Length];
        for (int instrIndex = 0; instrIndex < code.Length; instrIndex++)
        {
            var instruction = code[instrIndex];
            var args = instruction.Args;
            object data;
            var opcode = instruction.Opcode;

            switch (instruction.Opcode)
            {
                case JavaOpcode.nop:
                case JavaOpcode.aconst_null:
                case JavaOpcode.iconst_m1:
                case JavaOpcode.iconst_0:
                case JavaOpcode.iconst_1:
                case JavaOpcode.iconst_2:
                case JavaOpcode.iconst_3:
                case JavaOpcode.iconst_4:
                case JavaOpcode.iconst_5:
                case JavaOpcode.lconst_0:
                case JavaOpcode.lconst_1:
                case JavaOpcode.fconst_0:
                case JavaOpcode.fconst_1:
                case JavaOpcode.fconst_2:
                case JavaOpcode.dconst_0:
                case JavaOpcode.dconst_1:
                    data = null!;
                    break;
                case JavaOpcode.bipush:
                {
                    sbyte b = unchecked((sbyte)args[0]);
                    data = (int)b;
                    break;
                }
                case JavaOpcode.sipush:
                    data = Combine(args[0], args[1]);
                    break;
                case JavaOpcode.ldc:
                    data = consts[args[0]];
                    break;
                case JavaOpcode.ldc_w:
                case JavaOpcode.ldc2_w:
                    data = consts[Combine(args[0], args[1])];
                    break;
                case JavaOpcode.iload:
                case JavaOpcode.lload:
                case JavaOpcode.fload:
                case JavaOpcode.dload:
                case JavaOpcode.aload:
                    data = (int)args[0];
                    break;
                case JavaOpcode.iload_0:
                case JavaOpcode.iload_1:
                case JavaOpcode.iload_2:
                case JavaOpcode.iload_3:
                case JavaOpcode.lload_0:
                case JavaOpcode.lload_1:
                case JavaOpcode.lload_2:
                case JavaOpcode.lload_3:
                case JavaOpcode.fload_0:
                case JavaOpcode.fload_1:
                case JavaOpcode.fload_2:
                case JavaOpcode.fload_3:
                case JavaOpcode.dload_0:
                case JavaOpcode.dload_1:
                case JavaOpcode.dload_2:
                case JavaOpcode.dload_3:
                case JavaOpcode.aload_0:
                case JavaOpcode.aload_1:
                case JavaOpcode.aload_2:
                case JavaOpcode.aload_3:
                case JavaOpcode.iaload:
                case JavaOpcode.laload:
                case JavaOpcode.faload:
                case JavaOpcode.daload:
                case JavaOpcode.aaload:
                case JavaOpcode.baload:
                case JavaOpcode.caload:
                case JavaOpcode.saload:
                    data = null!;
                    break;
                case JavaOpcode.istore:
                case JavaOpcode.lstore:
                case JavaOpcode.fstore:
                case JavaOpcode.dstore:
                case JavaOpcode.astore:
                    data = (int)args[0];
                    break;
                case JavaOpcode.istore_0:
                case JavaOpcode.istore_1:
                case JavaOpcode.istore_2:
                case JavaOpcode.istore_3:
                case JavaOpcode.lstore_0:
                case JavaOpcode.lstore_1:
                case JavaOpcode.lstore_2:
                case JavaOpcode.lstore_3:
                case JavaOpcode.fstore_0:
                case JavaOpcode.fstore_1:
                case JavaOpcode.fstore_2:
                case JavaOpcode.fstore_3:
                case JavaOpcode.dstore_0:
                case JavaOpcode.dstore_1:
                case JavaOpcode.dstore_2:
                case JavaOpcode.dstore_3:
                case JavaOpcode.astore_0:
                case JavaOpcode.astore_1:
                case JavaOpcode.astore_2:
                case JavaOpcode.astore_3:
                case JavaOpcode.iastore:
                case JavaOpcode.lastore:
                case JavaOpcode.fastore:
                case JavaOpcode.dastore:
                case JavaOpcode.aastore:
                case JavaOpcode.bastore:
                case JavaOpcode.castore:
                case JavaOpcode.sastore:
                    data = null!;
                    break;
                case JavaOpcode.pop:
                case JavaOpcode.pop2:
                case JavaOpcode.dup:
                case JavaOpcode.dup_x1:
                case JavaOpcode.dup_x2:
                case JavaOpcode.dup2:
                case JavaOpcode.dup2_x1:
                case JavaOpcode.dup2_x2:
                case JavaOpcode.swap:
                    data = null!;
                    break;
                case JavaOpcode.iadd:
                case JavaOpcode.ladd:
                case JavaOpcode.fadd:
                case JavaOpcode.dadd:
                case JavaOpcode.isub:
                case JavaOpcode.lsub:
                case JavaOpcode.fsub:
                case JavaOpcode.dsub:
                case JavaOpcode.imul:
                case JavaOpcode.lmul:
                case JavaOpcode.fmul:
                case JavaOpcode.dmul:
                case JavaOpcode.idiv:
                case JavaOpcode.ldiv:
                case JavaOpcode.fdiv:
                case JavaOpcode.ddiv:
                case JavaOpcode.irem:
                case JavaOpcode.lrem:
                case JavaOpcode.frem:
                case JavaOpcode.drem:
                case JavaOpcode.ineg:
                case JavaOpcode.lneg:
                case JavaOpcode.fneg:
                case JavaOpcode.dneg:
                case JavaOpcode.ishl:
                case JavaOpcode.lshl:
                case JavaOpcode.ishr:
                case JavaOpcode.lshr:
                case JavaOpcode.iushr:
                case JavaOpcode.lushr:
                case JavaOpcode.iand:
                case JavaOpcode.land:
                case JavaOpcode.ior:
                case JavaOpcode.lor:
                case JavaOpcode.ixor:
                case JavaOpcode.lxor:
                    data = null!;
                    break;
                case JavaOpcode.iinc:
                    data = new int[] { args[0], args[1] };
                    break;
                case JavaOpcode.i2l:
                case JavaOpcode.i2f:
                case JavaOpcode.i2d:
                case JavaOpcode.l2i:
                case JavaOpcode.l2f:
                case JavaOpcode.l2d:
                case JavaOpcode.f2i:
                case JavaOpcode.f2l:
                case JavaOpcode.f2d:
                case JavaOpcode.d2i:
                case JavaOpcode.d2l:
                case JavaOpcode.d2f:
                case JavaOpcode.i2b:
                case JavaOpcode.i2c:
                case JavaOpcode.i2s:
                case JavaOpcode.lcmp:
                case JavaOpcode.fcmpl:
                case JavaOpcode.fcmpg:
                case JavaOpcode.dcmpl:
                case JavaOpcode.dcmpg:
                    data = null!;
                    break;
                case JavaOpcode.ifeq:
                case JavaOpcode.ifne:
                case JavaOpcode.iflt:
                case JavaOpcode.ifge:
                case JavaOpcode.ifgt:
                case JavaOpcode.ifle:
                case JavaOpcode.if_icmpeq:
                case JavaOpcode.if_icmpne:
                case JavaOpcode.if_icmplt:
                case JavaOpcode.if_icmpge:
                case JavaOpcode.if_icmpgt:
                case JavaOpcode.if_icmple:
                case JavaOpcode.if_acmpeq:
                case JavaOpcode.if_acmpne:
                case JavaOpcode.@goto:
                {
                    var ros = Combine(args[0], args[1]);
                    var os = ros + instruction.Offset;
                    if (offsets.TryGetValue(os, out var opcodeNum))
                        data = opcodeNum;
                    else
                    {
                        var offsetsPrint = string.Join('\n',
                            offsets.Select(x => $"{x.Value}: {x.Key} ({code[x.Value].Opcode})"));
                        throw new JavaLinkageException(
                            $"There is no opcode at offset {os} (relative {ros}).\nAvailable offsets:\n{offsetsPrint}");
                    }

                    break;
                }
                case JavaOpcode.jsr:
                case JavaOpcode.ret:
                    data = null!;
                    break;
                case JavaOpcode.tableswitch:
                {
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

                    for (int j = 0; j < count; j++)
                    {
                        var off = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                        d[j + 3] = offsets[off + instruction.Offset];
                        i += 4;
                    }

                    data = d;
                    break;
                }
                case JavaOpcode.lookupswitch:
                {
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
                    for (int j = 0; j < count; j++)
                    {
                        d[2 + j * 2] = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                        i += 4;
                        var off = (args[i] << 24) | (args[i + 1] << 16) | (args[i + 2] << 8) | args[i + 3];
                        d[3 + j * 2] = offsets[off + instruction.Offset];
                        i += 4;
                    }

                    data = d;
                    break;
                }
                case JavaOpcode.ireturn:
                case JavaOpcode.lreturn:
                case JavaOpcode.freturn:
                case JavaOpcode.dreturn:
                case JavaOpcode.areturn:
                case JavaOpcode.@return:
                {
                    if (isClinit)
                        opcode = JavaOpcode._inplacereturn;
                    data = null!;
                    break;
                }
                case JavaOpcode.getstatic:
                {
                    var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    var c = jvm.Classes[d.ClassName];
                    var f = c.GetFieldRecursive(d.Descriptor);
                    var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                    data = new FieldPointer(b, c);
                    break;
                }
                case JavaOpcode.putstatic:
                {
                    var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    var c = jvm.Classes[d.ClassName];
                    var f = c.GetFieldRecursive(d.Descriptor);
                    var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                    data = new FieldPointer(b, c);
                    break;
                }
                case JavaOpcode.getfield:
                {
                    var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    var c = jvm.Classes[d.ClassName];
                    var f = c.GetFieldRecursive(d.Descriptor);
                    var b = f.GetValue ?? throw new JavaLinkageException("Not get bridge!");
                    data = new FieldPointer(b, c);
                    break;
                }
                case JavaOpcode.putfield:
                {
                    var d = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    var c = jvm.Classes[d.ClassName];
                    var f = c.GetFieldRecursive(d.Descriptor);
                    var b = f.SetValue ?? throw new JavaLinkageException("Not set bridge!");
                    data = new FieldPointer(b, c);
                    break;
                }
                case JavaOpcode.invokevirtual:
                {
                    data = LinkVirtualCall(jvm, consts, args);
                    break;
                }
                case JavaOpcode.invokespecial:
                case JavaOpcode.invokestatic:
                {
                    var ndc = (NameDescriptorClass)consts[Combine(args[0], args[1])];
                    var @class = jvm.Classes[ndc.ClassName];
                    var m = @class.GetMethodRecursive(ndc.Descriptor);
                    data = m;
                    break;
                }
                case JavaOpcode.invokeinterface:
                {
                    data = LinkVirtualCall(jvm, consts, args);
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
                    break;
                }
                case JavaOpcode.newarray:
                    data = (ArrayType)args[0];
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
                    break;
                }
                case JavaOpcode.arraylength:
                case JavaOpcode.athrow:
                    data = null!;
                    break;
                case JavaOpcode.checkcast:
                case JavaOpcode.instanceof:
                {
                    var type = (string)consts[Combine(args[0], args[1])];
                    data = jvm.GetClass(type);
                    break;
                }
                case JavaOpcode.monitorenter:
                case JavaOpcode.monitorexit:
                    data = null!;
                    break;
                case JavaOpcode.wide:
                    data = args; // let's parse this in runtime
                    break;
                case JavaOpcode.multianewarray:
                {
                    var dims = args[2];
                    var type = (string)consts[Combine(args[0], args[1])];
                    data = new MultiArrayInitializer(dims, jvm.GetClass(type));
                    break;
                }
                case JavaOpcode.ifnull:
                case JavaOpcode.ifnonnull:
                    data = offsets[Combine(args[0], args[1]) + instruction.Offset];
                    break;
                case JavaOpcode.goto_w:
                case JavaOpcode.jsr_w:
                case JavaOpcode.breakpoint:
                    data = null!;
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

            output[instrIndex] = new LinkedInstruction(opcode, data);
        }

        return output;
    }

    private static object LinkVirtualCall(JvmState jvm, object[] consts, byte[] args)
    {
        object data;
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

        data = new VirtualPointer(jvm.GetVirtualPointer(nd),
            DescriptorUtils.ParseMethodArgsCount(nd.Descriptor));
        return data;
    }

    public static int Combine(byte indexByte1, byte indexByte2)
    {
        var u = (ushort)((indexByte1 << 8) | indexByte2);
        var s = (short)u;
        return s;
    }
}