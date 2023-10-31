using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Be.IO;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;

namespace MahoTrans.Loader;

/// <summary>
/// Set of tools to take useful things from JAR file.
/// </summary>
public static class ClassLoader
{
    /// <summary>
    /// Reads JAR package.
    /// </summary>
    /// <param name="file">Actual file stream.</param>
    /// <param name="leaveOpen">True to leave stream opened.</param>
    /// <param name="logger">Logger to print info into.</param>
    /// <returns>JAR object.</returns>
    public static JarPackage ReadJar(Stream file, bool leaveOpen, ILogger logger)
    {
        using (var zip = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen, Encoding.UTF8))
        {
            List<JavaClass> classes = new();
            Dictionary<string, byte[]> res = new();
            Dictionary<string, string>? manifest = null;
            var files = zip.Entries;
            foreach (var entry in files)
            {
                using (var r = entry.Open())
                {
                    List<byte> content = new List<byte>((int)entry.Length);
                    byte[] buf = new byte[512];
                    int read;
                    while (true)
                    {
                        read = r.Read(buf);
                        if (read == 0)
                            break;
                        content.AddRange(buf.Take(read));
                    }

                    res.Add(entry.FullName, content.ToArray());
                }

                if (entry.Name.EndsWith(".class"))
                {
                    using var stream = entry.Open();
                    var cls = ClassLoader.Read(stream, true);
                    classes.Add(cls);
                }

                if (entry.Name == "META-INF/MANIFEST.MF")
                {
                    //TODO check MIDP docs
                    manifest = Encoding.UTF8.GetString(res["META-INF/MANIFEST.MF"])
                        .Split('\n')
                        .Select(x => x.Trim('\r').Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(x => x.Split(':', 2,
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .Where(x => x.Length == 2)
                        .ToDictionary(x => x[0], x => x[1]);
                }
            }

            if (manifest == null)
            {
                logger.PrintLoadTime(LogLevel.Error, "Manifest file was not found");
                manifest = new()
                {
                    { "MIDlet-Version", "1.0.0" }
                };
            }

            return new JarPackage(classes.ToArray(), res, manifest);
        }
    }

    public static JavaClass Read(Stream file, bool leaveOpen)
    {
        using (var reader = new BeBinaryReader(file, Encoding.UTF8, leaveOpen))
        {
            var c = new JavaClass();

            ReadHeader(c, reader);
            ReadConstants(c, reader);
            ReadInfo(c, reader);
            ReadInterfaces(c, reader);
            ReadFields(c, reader);
            ReadMethods(c, reader);

            return c;
        }
    }

    private static ReadOnlyDictionary<JavaOpcode, int> _opcodeArgsCache = BuildOpcodeCache();

    private static ReadOnlyDictionary<JavaOpcode, int> BuildOpcodeCache()
    {
        Dictionary<JavaOpcode, int> dict = new();
        foreach (var opcode in Enum.GetValues<JavaOpcode>())
        {
            var attr = typeof(JavaOpcode).GetField(opcode.ToString())?.GetCustomAttribute<OpcodeArgsCountAttribute>();
            int count = attr?.ArgsCount ?? 0;
            dict.Add(opcode, count);
        }

        return new ReadOnlyDictionary<JavaOpcode, int>(dict);
    }

    private static void ReadHeader(JavaClass c, BeBinaryReader reader)
    {
        var magic = reader.ReadInt32();
        //TODO check magic
        c.MinorVersion = reader.ReadInt16();
        c.MajorVersion = reader.ReadInt16();
    }

    private static void ReadConstants(JavaClass rc, BeBinaryReader reader)
    {
        int count = reader.ReadInt16();
        object?[] consts = new object?[count];

        for (int i = 1; i < count; i++)
        {
            var ct = (ConstantType)reader.ReadByte();

            switch (ct)
            {
                case ConstantType.CONSTANT_Class:
                    consts[i] = new StringReference(reader.ReadInt16());
                    break;
                case ConstantType.CONSTANT_Fieldref:
                    consts[i] = new MemberReference(reader);
                    break;
                case ConstantType.CONSTANT_Methodref:
                    consts[i] = new MemberReference(reader);
                    break;
                case ConstantType.CONSTANT_InterfaceMethodref:
                    consts[i] = new MemberReference(reader);
                    break;
                case ConstantType.CONSTANT_String:
                    consts[i] = new StringReference(reader.ReadInt16());
                    break;
                case ConstantType.CONSTANT_Integer:
                    consts[i] = reader.ReadInt32();
                    break;
                case ConstantType.CONSTANT_Float:
                    consts[i] = reader.ReadSingle();
                    break;
                case ConstantType.CONSTANT_Long:
                    consts[i] = reader.ReadInt64();
                    i++;
                    break;
                case ConstantType.CONSTANT_Double:
                    consts[i] = reader.ReadDouble();
                    i++;
                    break;
                case ConstantType.CONSTANT_NameAndType:
                    consts[i] = new NameTypeReference(reader);
                    break;
                case ConstantType.CONSTANT_Utf8:
                    consts[i] = ReadUtf(reader);
                    break;
                case ConstantType.CONSTANT_MethodHandle:
                    throw new NotSupportedException();
                case ConstantType.CONSTANT_MethodType:
                    consts[i] = new StringReference(reader.ReadInt16());
                    break;
                case ConstantType.CONSTANT_InvokeDynamic:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        for (var i = 0; i < consts.Length; i++)
        {
            var @const = consts[i];
            switch (@const)
            {
                case StringReference strRef:
                    consts[i] = consts[strRef.Index];
                    break;
                case NameTypeReference ntRef:
                {
                    var name = consts[ntRef.NameIndex] as string ??
                               throw new JavaLinkageException("Name part of descriptor was not a string");
                    var d = consts[ntRef.DescriptorIndex] as string ??
                            throw new JavaLinkageException("Descriptor part of descriptor was not a string");
                    consts[i] = new NameDescriptor(name,
                        d);
                    break;
                }
            }
        }

        object[] linkedConsts = new object[count];

        for (int i = 0; i < count; i++)
        {
            var @const = consts[i];
            if (@const is null)
            {
                linkedConsts[i] = new NullConstant();
            }
            else if (@const is MemberReference mr)
            {
                var ntb = consts[mr.NameTypeIndex];
                if (ntb is not NameDescriptor nt)
                {
                    throw new JavaLinkageException($"ND part for NDC was not a ND (constant #{mr.NameTypeIndex})");
                }

                var cls = consts[mr.ClassIndex] as string ??
                          throw new JavaLinkageException(
                              $"Class name part for NDC was not a string (constant #{mr.ClassIndex})");
                linkedConsts[i] = new NameDescriptorClass(nt, cls);
            }
            else
            {
                linkedConsts[i] = @const;
            }
        }

        rc.Constants = linkedConsts;
    }

    private static void ReadInfo(JavaClass rc, BeBinaryReader reader)
    {
        rc.Flags = (ClassFlags)reader.ReadInt16();
        var clsNameIndex = reader.ReadInt16();
        rc.Name = rc.Constants[clsNameIndex] as string ??
                  throw new JavaLinkageException($"Class name was not a string (constant #{clsNameIndex})");
        var clsSuper = reader.ReadInt16();
        rc.SuperName = rc.Constants[clsSuper] as string ??
                       throw new JavaLinkageException($"Class super was not a string (constant #{clsSuper})");
    }

    private static void ReadInterfaces(JavaClass rc, BeBinaryReader reader)
    {
        int count = reader.ReadInt16();
        var a = new string[count];
        for (int i = 0; i < count; i++)
        {
            var ii = reader.ReadInt16();
            a[i] = rc.Constants[ii] as string ??
                   throw new JavaLinkageException($"Interface name was not a string (constant #{ii})");
        }

        rc.Interfaces = a;
    }

    private static void ReadFields(JavaClass rc, BeBinaryReader reader)
    {
        int count = reader.ReadInt16();
        var a = new Field[count];
        for (int i = 0; i < count; i++)
        {
            var flags = (FieldFlags)reader.ReadInt16();
            var nd = ReadMemberND(reader, rc.Constants);
            var attrsCount = reader.ReadInt16();
            var attrs = new JavaAttribute[attrsCount];
            for (int j = 0; j < attrsCount; j++)
            {
                string attrType = ReadAttributeName(reader, rc.Constants);
                int len = reader.ReadInt32();
                attrs[j] = new JavaAttribute(attrType)
                {
                    Data = reader.ReadBytes(len)
                };
            }

            a[i] = new Field(nd, flags)
            {
                Attributes = attrs
            };
        }

        rc.Fields = a.ToDictionary(x => x.Descriptor, x => x);
    }

    private static void ReadMethods(JavaClass rc, BeBinaryReader reader)
    {
        int count = reader.ReadInt16();
        var a = new Method[count];
        for (int i = 0; i < count; i++)
        {
            var flags = (MethodFlags)reader.ReadInt16();
            var nd = ReadMemberND(reader, rc.Constants);
            Method m = new(nd, flags, rc);
            int attrsCount = reader.ReadInt16();
            List<JavaAttribute> attrs = new(attrsCount);

            for (int j = 0; j < attrsCount; j++)
            {
                string attrType = ReadAttributeName(reader, rc.Constants);
                int len = reader.ReadInt32();
                byte[] data = reader.ReadBytes(len);
                if (attrType.Equals("Code"))
                {
                    m.JavaBody = ReadCode(data, rc.Constants);
                }
                else
                {
                    attrs.Add(new JavaAttribute(attrType)
                    {
                        Data = data
                    });
                }
            }

            m.Attributes = attrs.ToArray();

            a[i] = m;
        }

        rc.Methods = a.ToDictionary(x => x.Descriptor, x => x);
    }

    private static string ReadAttributeName(BeBinaryReader stream, object[] consts)
    {
        var attrNameIndex = stream.ReadInt16();
        if (consts[attrNameIndex] is string attrType)
            return attrType;
        throw new JavaLinkageException($"Attribute name was not a string (constant #{attrNameIndex})");
    }

    private static NameDescriptor ReadMemberND(BeBinaryReader stream, object[] consts)
    {
        var nameIndex = stream.ReadInt16();
        var descriptorIndex = stream.ReadInt16();

        if (consts[nameIndex] is not string name)
        {
            throw new JavaLinkageException($"Member name was not a string (constant #{nameIndex})");
        }

        if (consts[descriptorIndex] is not string descriptor)
        {
            throw new JavaLinkageException($"Member descriptor was not a string (constant #{descriptorIndex})");
        }

        return new NameDescriptor(name, descriptor);
    }

    private static string ReadUtf(BeBinaryReader stream)
    {
        int len = stream.ReadInt16();
        byte[] data = stream.ReadBytes(len);
        return data.DecodeJavaUnicode();
    }


    private static JavaMethodBody ReadCode(byte[] data, object[] consts)
    {
        using (MemoryStream ms = new MemoryStream(data, false))
        {
            using (BeBinaryReader r = new BeBinaryReader(ms, Encoding.Default, true))
            {
                var stack = r.ReadInt16();
                var locals = r.ReadInt16();
                int len = r.ReadInt32();
                byte[] code = r.ReadBytes(len);
                int exLen = r.ReadInt16();
                var exs = new JavaMethodBody.Catch[exLen];
                for (int i = 0; i < exLen; i++)
                {
                    exs[i] = new JavaMethodBody.Catch(r.ReadInt16(), r.ReadInt16(), r.ReadInt16(), r.ReadInt16());
                }

                int attrLen = r.ReadInt16();
                var attrs = new JavaAttribute[attrLen];
                for (int i = 0; i < attrLen; i++)
                {
                    string attrType = ReadAttributeName(r, consts);
                    int alen = r.ReadInt32();
                    attrs[i] = new JavaAttribute(attrType)
                    {
                        Data = r.ReadBytes(alen)
                    };
                }

                List<Instruction> instrs = new List<Instruction>();

                {
                    int i = 0;
                    while (i < code.Length)
                    {
                        int offset = i;
                        JavaOpcode op = (JavaOpcode)code[offset];
                        i++;
                        _opcodeArgsCache.TryGetValue(op, out int args);
                        if (args == 0)
                        {
                            instrs.Add(new Instruction(offset, op));
                            continue;
                        }

                        byte[] c;

                        if (args == -1)
                        {
                            switch (op)
                            {
                                case JavaOpcode.wide:
                                {
                                    args = code[i] == (int)JavaOpcode.iinc ? 5 : 3;
                                    break;
                                }
                                case JavaOpcode.tableswitch:
                                {
                                    var aligning = i % 4;
                                    if (aligning == 0)
                                        aligning = 4;
                                    args = 4 - aligning;

                                    args += 4; // default
                                    int low = (code[i + args] << 24) | (code[i + args + 1] << 16) |
                                              (code[i + args + 2] << 8) | code[i + args + 3];
                                    args += 4; // low
                                    int high = (code[i + args] << 24) | (code[i + args + 1] << 16) |
                                               (code[i + args + 2] << 8) | code[i + args + 3];
                                    args += 4; // high
                                    args += (high - low + 1) * 4;
                                    break;
                                }
                                case JavaOpcode.lookupswitch:
                                {
                                    var aligning = i % 4;
                                    if (aligning == 0)
                                        aligning = 4;
                                    args = 4 - aligning;

                                    args += 4; // default
                                    int count = (code[i + args] << 24) | (code[i + args + 1] << 16) |
                                                (code[i + args + 2] << 8) | code[i + args + 3];
                                    args += 4; //count
                                    args += count * 8;
                                    break;
                                }
                                default:
                                    args = 0;
                                    break;
                            }
                        }

                        c = new byte[args];
                        for (int j = 0; j < args; j++)
                        {
                            c[j] = code[i + j];
                        }

                        i += args;

                        instrs.Add(new Instruction(offset, op, c));
                    }
                }

                return new JavaMethodBody
                {
                    StackSize = stack,
                    LocalsCount = locals,
                    Code = instrs.ToArray(),
                    Catches = exs,
                    Attrs = attrs
                };
            }
        }
    }

    private class StringReference
    {
        public readonly int Index;

        public StringReference(int index)
        {
            Index = index;
        }
    }

    private class NameTypeReference
    {
        public readonly short NameIndex;
        public readonly short DescriptorIndex;

        public NameTypeReference(BeBinaryReader stream)
        {
            NameIndex = stream.ReadInt16();
            DescriptorIndex = stream.ReadInt16();
        }
    }

    private class MemberReference
    {
        public readonly short ClassIndex;
        public readonly short NameTypeIndex;

        public MemberReference(BeBinaryReader stream)
        {
            ClassIndex = stream.ReadInt16();
            NameTypeIndex = stream.ReadInt16();
        }
    }

    public static Dictionary<JavaOpcode, int> CountOpcodes(IEnumerable<JavaClass> classes)
    {
        var dict = Enum.GetValues<JavaOpcode>().ToDictionary(x => x, _ => 0);
        foreach (var @class in classes)
        {
            foreach (var method in @class.Methods.Values)
            {
                if (method.IsNative)
                    continue;
                if (method.JavaBody == null!)
                    continue;
                foreach (var instruction in method.JavaBody.Code)
                    dict[instruction.Opcode] += 1;
            }
        }

        return dict;
    }
}