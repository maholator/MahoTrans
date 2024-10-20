// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using java.lang;
using MahoTrans.Abstractions;
using MahoTrans.Utils;
using Array = System.Array;
using Object = java.lang.Object;

namespace MahoTrans.Runtime.Types;

public class JavaClass : IJavaEntity
{
    public short MinorVersion;
    public short MajorVersion;
    public object[] Constants = Array.Empty<object>();
    public ClassFlags Flags;
    public string Name { get; set; } = "java/lang/Object";
    public string SuperName = "java/lang/Object";
    public JavaClass Super = null!;
    public string[] Interfaces = Array.Empty<string>();
    public Dictionary<NameDescriptor, Field> Fields = new();
    public Dictionary<NameDescriptor, Method> Methods = new();
    public Method?[]? VirtualTable;
    public Dictionary<int, Method>? VirtualTableMap;
    public Type? ClrType;

    /// <summary>
    ///     This is set by first call of <see cref="java.lang.Object.getClass" />.
    /// </summary>
    public Reference ModelObject;

    /// <summary>
    ///     True, if this class' initializer was not yet executed.
    /// </summary>
    public bool PendingInitializer = true;

    public int Size;

    public string? DisplayableName { get; set; }

    public override string ToString() => Name;

    public bool IsArray => Name[0] == '[';

    public uint GetSnapshotHash()
    {
        uint interfacesHash = (uint)Interfaces.Length;
        foreach (var inter in Interfaces.OrderBy(x => x))
        {
            interfacesHash ^= inter.GetSnapshotHash();
        }

        uint fieldsHash = 0;
        foreach (var value in Fields.Values)
        {
            fieldsHash ^= value.GetSnapshotHash();
        }

        uint methodsHash = 0;
        foreach (var value in Methods.Values.OrderBy(x => x.Descriptor.Name).ThenBy(x => x.Descriptor.Descriptor))
        {
            methodsHash ^= value.GetSnapshotHash();
        }

        //TODO constants are not checked because they may be different each compilation
        return fieldsHash ^ methodsHash ^ interfacesHash ^ SuperName.GetSnapshotHash() ^ (uint)Constants.Length;
    }

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

    /// <summary>
    ///     True if this class is <see cref="Object" />.
    /// </summary>
    public bool IsObject => Name == "java/lang/Object";

    public bool IsInterface => (Flags & ClassFlags.Interface) != 0;

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
            dict = new Dictionary<int, Method>(Super.VirtualTableMap!);
        }

        dict ??= new Dictionary<int, Method>();

        foreach (var method in Methods)
        {
            dict[jvm.GetVirtualPointer(method.Key)] = method.Value;
        }

        VirtualTableMap = dict;

        if (dict.Count == 0)
        {
            VirtualTable = Array.Empty<Method>();
            return;
        }

        var arr = new Method?[dict.Keys.Max() + 1];
        foreach (var kvp in dict)
        {
            arr[kvp.Key] = kvp.Value;
        }

        VirtualTable = arr;
    }

    public List<Field> GetAllFieldsRecursive()
    {
        List<Field> list = new();

        list.AddRange(Fields.Values);
        if (Super != null! && Name != "java/lang/Object")
            list.AddRange(Super.GetAllFieldsRecursive());

        return list;
    }

    public void RecalculateSize()
    {
        int size = 20; // base object size
        var cls = this;
        while (true)
        {
            foreach (var field in cls.Fields.Values)
            {
                size += field.Descriptor.Descriptor[0] switch
                {
                    'B' => 1,
                    'Z' => 1,
                    'S' => 2,
                    'C' => 2,
                    'I' => 4,
                    'J' => 8,
                    'F' => 4,
                    'D' => 8,
                    '[' => 4,
                    'L' => 4,
                    _ => throw new ArgumentException()
                };
            }

            if (cls.IsObject)
            {
                Size = size;
                return;
            }

            cls = cls.Super;
        }
    }

    /// <summary>
    ///     Gets field defined on this class or one of its supers. This assumes that class tree already built.
    /// </summary>
    /// <param name="descriptor">Descriptor of the field.</param>
    /// <returns>Field object and class where the field lives. Null if field was not found.</returns>
    public (JavaClass, Field)? GetFieldRecursiveOrNull(NameDescriptor descriptor)
    {
        var cls = this;
        while (true)
        {
            if (cls.Fields.TryGetValue(descriptor, out var f))
                return (cls, f);
            if (cls.IsObject)
                return null;
            cls = cls.Super;
        }
    }

    /// <summary>
    ///     Gets method defined on this class or one of its supers. This assumes that class tree already built.
    /// </summary>
    /// <param name="descriptor">Descriptor of the field.</param>
    /// <returns>Method object.</returns>
    public Method? GetMethodRecursiveOrNull(NameDescriptor descriptor)
    {
        var cls = this;
        while (true)
        {
            if (cls.Methods.TryGetValue(descriptor, out var m))
            {
                Debug.Assert(m.Class == cls);
                return m;
            }

            if (cls.IsInterface)
            {
                foreach (var interf in cls.Interfaces)
                {
                    var m2 = JvmContext.Jvm!.GetLoadedClassOrNull(interf)?.GetMethodRecursiveOrNull(descriptor);
                    if (m2 != null) return m2;
                }
            }

            if (cls.IsObject)
                return null;
            cls = cls.Super;
        }
    }

    /// <summary>
    ///     Runs class' initializer method on the thread. Call this before any usage. Call this only once per class lifecycle.
    ///     This must be called inside JVM context.
    /// </summary>
    /// <param name="thread">Thread to run initialization on.</param>
    public void Initialize(JavaThread thread)
    {
        PendingInitializer = false;

        var m = ClassInitMethod;

        if (m != null)
        {
            Object.Jvm.Toolkit.Logger?.LogEvent(EventCategory.ClassInitializer,
                $"Class {Name} initialized via <clinit> method");
            if (m.Bridge != null)
            {
                m.Bridge(null!);
            }
            else
            {
                thread.Push(m.JavaBody!);
            }
        }
        else
        {
            Object.Jvm.Toolkit.Logger?.LogEvent(EventCategory.ClassInitializer,
                $"Class {Name} has no initialization method");
        }
    }

    public Method? ClassInitMethod => Methods.GetValueOrDefault(NameDescriptor.ClassInit);

    public Reference GetOrInitModel()
    {
        if (ModelObject.IsNull)
        {
            var cls = JvmContext.Jvm!.Allocate<Class>();
            cls.InternalClass = this;
            ModelObject = cls.This;
        }

        return ModelObject;
    }
}
