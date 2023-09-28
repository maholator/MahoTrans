using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Be.IO;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Loader;

public static class ClassLoader
{
    public static (JavaClass[], Dictionary<string, byte[]>) ReadJar(Stream file, bool leaveOpen)
    {
        using (var zip = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen, Encoding.UTF8))
        {
            List<JavaClass> classes = new();
            Dictionary<string, byte[]> res = new();
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
            }

            return (classes.ToArray(), res);
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
        c.Magic = reader.ReadInt32();
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
                    consts[i] = new NameDescriptor(consts[ntRef.NameIndex] as string,
                        consts[ntRef.DescrIndex] as string);
                    break;
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
                var nt = (NameDescriptor)consts[mr.NameTypeIndex];
                linkedConsts[i] = new NameDescriptorClass(nt, (string)consts[mr.ClassIndex]);
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
        rc.Name = rc.Constants[reader.ReadInt16()] as string;
        rc.SuperName = rc.Constants[reader.ReadInt16()] as string;
    }

    private static void ReadInterfaces(JavaClass rc, BeBinaryReader reader)
    {
        int count = reader.ReadInt16();
        var a = new string[count];
        for (int i = 0; i < count; i++)
        {
            a[i] = rc.Constants[reader.ReadInt16()] as string;
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
            var name = rc.Constants[reader.ReadInt16()] as string;
            var descr = rc.Constants[reader.ReadInt16()] as string;
            var attrsCount = reader.ReadInt16();
            var attrs = new JavaAttribute[attrsCount];
            for (int j = 0; j < attrsCount; j++)
            {
                string attrType = rc.Constants[reader.ReadInt16()] as string;
                int len = reader.ReadInt32();
                attrs[j] = new JavaAttribute(attrType)
                {
                    Data = reader.ReadBytes(len)
                };
            }

            a[i] = new Field(new NameDescriptor(name, descr), flags)
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
            var name = rc.Constants[reader.ReadInt16()] as string;
            var descr = rc.Constants[reader.ReadInt16()] as string;
            Method m = new(new NameDescriptor(name, descr), flags, rc);
            int attrsCount = reader.ReadInt16();
            List<JavaAttribute> attrs = new(attrsCount);

            for (int j = 0; j < attrsCount; j++)
            {
                string attrType = rc.Constants[reader.ReadInt16()] as string;
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

    private static string ReadUtf(BeBinaryReader stream)
    {
        int len = stream.ReadInt16();
        byte[] data = stream.ReadBytes(len);
        return data.DecodeJavaUnicode();
    }

    

    private static JavaMethodBody ReadCode(byte[] data, object?[] consts)
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
                    string attrType = consts[r.ReadInt16()] as string;
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
        public readonly short DescrIndex;

        public NameTypeReference(BeBinaryReader stream)
        {
            NameIndex = stream.ReadInt16();
            DescrIndex = stream.ReadInt16();
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