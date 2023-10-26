using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MahoTrans.Runtime.Types;

public class JavaClass
{
    public int Magic;
    public short MinorVersion;
    public short MajorVersion;
    public object[] Constants = Array.Empty<object>();
    public ClassFlags Flags;
    public string Name = "java/lang/Object";
    public string SuperName = "java/lang/Object";
    public JavaClass Super = null!;
    public string[] Interfaces = Array.Empty<string>();
    public Dictionary<NameDescriptor, Field> Fields = new();
    public Dictionary<NameDescriptor, Method> Methods = new();
    public Dictionary<int, Method>? VirtualTable;
    public Type? ClrType;
    public bool PendingInitializer = true;
    public Action<List<Reference>>? StaticAnnouncer;

    public override string ToString() => Name;

    public TypeAttributes ClrFlags
    {
        get
        {
            TypeAttributes a = 0;
            if (Flags.HasFlag(ClassFlags.Public))
                a |= TypeAttributes.Public;
            if (Flags.HasFlag(ClassFlags.Final))
                a |= TypeAttributes.Sealed;
            if (Flags.HasFlag(ClassFlags.Interface))
                a |= TypeAttributes.Interface;
            if (Flags.HasFlag(ClassFlags.Abstract))
                a |= TypeAttributes.Abstract;
            return a;
        }
    }

    public int PushConstant(object value)
    {
        lock (this)
        {
            int i = Constants.Length;
            Constants = Constants.Append(value).ToArray();
            return i;
        }
    }

    public bool Is(string type)
    {
        if (Name == type)
            return true;
        JavaClass jc = this;

        while (true)
        {
            if (jc.SuperName == type)
                return true;
            if (jc.Super.IsObject)
                return false;
            jc = jc.Super;
        }
    }

    public bool Is(JavaClass type)
    {
        if (this == type)
            return true;

        if (IsObject)
            return false;

        var name = type.Name;
        JavaClass jc = this;

        while (true)
        {
            if (jc.Super == type)
                return true;
            if (jc.Interfaces.Contains(name))
                return true;
            if (jc.Super.IsObject)
                return false;
            jc = jc.Super;
        }
    }

    public bool IsObject => Name == "java/lang/Object";

    [MemberNotNull(nameof(VirtualTable))]
    public void GenerateVirtualTable(JvmState jvm)
    {
        if (VirtualTable != null)
            return;

        Dictionary<int, Method>? dict = null;

        if (Name != "java/lang/Object")
        {
            if (Super.VirtualTable == null)
                Super.GenerateVirtualTable(jvm);
            dict = new Dictionary<int, Method>(Super.VirtualTable);
        }

        dict ??= new Dictionary<int, Method>();

        foreach (var method in Methods)
        {
            dict[jvm.GetVirtualPointer(method.Key)] = method.Value;
        }

        VirtualTable = dict;
    }

    /// <summary>
    /// Gets field defined on this class or one of its supers. This assumes that class tree already built.
    /// </summary>
    /// <param name="descriptor">Descriptor of the field.</param>
    /// <returns>Field object.</returns>
    public Field GetFieldRecursive(NameDescriptor descriptor)
    {
        var cls = this;
        while (true)
        {
            if (cls.Fields.TryGetValue(descriptor, out var f))
                return f;
            if (cls.IsObject)
                throw new JavaLinkageException($"Field {descriptor} is not found in class {this}");
            cls = cls.Super;
        }
    }

    /// <summary>
    /// Gets method defined on this class or one of its supers. This assumes that class tree already built.
    /// </summary>
    /// <param name="descriptor">Descriptor of the field.</param>
    /// <returns>Method object.</returns>
    public Method GetMethodRecursive(NameDescriptor descriptor)
    {
        var cls = this;
        while (true)
        {
            if (cls.Methods.TryGetValue(descriptor, out var m))
                return m;
            if (cls.IsObject)
                throw new JavaLinkageException($"Method {descriptor} is not found in class {this}");
            cls = cls.Super;
        }
    }

    public void AddMethod(Method m) => Methods.Add(m.Descriptor, m);

    /// <summary>
    /// Runs class' initializer method on the thread. Call this before any usage. Call this only once per class lifecycle. This must be called inside JVM context.
    /// </summary>
    /// <param name="thread">Thread to run initialization on.</param>
    public void Initialize(JavaThread thread)
    {
        PendingInitializer = false;

        if (Methods.TryGetValue(new NameDescriptor("<clinit>", "()V"), out var m))
        {
            if (m.Bridge != null)
            {
                m.Bridge(null!);
            }
            else
            {
                m.JavaBody.EnsureBytecodeLinked();
                thread.Push(m.JavaBody);
            }
        }
    }
}