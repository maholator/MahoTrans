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

    public static void VerifyCalls(JavaClass cls, JvmState jvm)
    {
        foreach (var method in cls.Methods.Values)
        {
            VerifyCalls(method, cls, jvm);
        }
    }

    private static void VerifyCalls(Method m, JavaClass cls, JvmState jvm)
    {
        if (m.IsNative)
            return;

        var consts = cls.Constants;
        var logger = jvm.Toolkit.Logger;

        Instruction[] code;

        try
        {
            code = m.JavaBody.Code;
        }
        catch
        {
            return;
        }

        foreach (var instruction in code)
        {
            var args = instruction.Args;
            switch (instruction.Opcode)
            {
                case JavaOpcode.newobject:
                {
                    var type = (string)consts[Combine(args[0], args[1])];
                    if (!jvm.Classes.TryGetValue(type, out var cls1))
                    {
                        logger.LogLoadtime(LogLevel.Warning, cls.Name,
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
                        logger.LogLoadtime(LogLevel.Warning, cls.Name,
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
                    else
                    {
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
                            logger.LogLoadtime(LogLevel.Warning, cls.Name,
                                $"\"{ndc.ClassName}\" has no method {ndc.Descriptor}");
                        }
                    }
                    else
                    {
                        logger.LogLoadtime(LogLevel.Warning, cls.Name,
                            $"\"{ndc.ClassName}\" can't be found but its method will be used");
                    }

                    break;
                }
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
                        throw new JavaLinkageException(
                            $"There is no opcode at offset {os} (relative {ros}).\nAvailable offsets:\n{string.Join('\n', offsets.Select(x => $"{x.Value}: {x.Key}"))}");
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