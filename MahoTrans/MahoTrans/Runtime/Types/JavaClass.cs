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

    public bool Is(string type, JvmState state)
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
    public void RegenerateVirtualTable(JvmState jvm)
    {
        Dictionary<int, Method>? dict = null;

        if (Name != "java/lang/Object")
        {
            if (Super.VirtualTable == null)
                Super.RegenerateVirtualTable(jvm);
            dict = new(Super.VirtualTable);
        }

        dict ??= new();

        foreach (var method in Methods)
        {
            dict[jvm.GetVirtualPointer(method.Key)] = method.Value;
        }

        VirtualTable = dict;
    }

    public Field? GetLocalField(NameDescriptor descriptor)
    {
        if (Fields.TryGetValue(descriptor, out var f))
            return f;
        return null;
    }

    public void AddMethod(Method m) => Methods.Add(m.Descriptor, m);

    /// <summary>
    /// Runs class' initializer method on the thread. Call this before any usage. Call this only once per class lifecycle.
    /// </summary>
    /// <param name="thread">Thread to run initialization on.</param>
    /// <param name="state">JVM.</param>
    public void Initialize(JavaThread thread, JvmState state)
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
                m.JavaBody.EnsureBytecodeLinked(state);
                thread.Push(m.JavaBody);
            }
        }
    }
}