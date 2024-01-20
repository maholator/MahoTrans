// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace java.lang;

public class Class : Object
{
    /// <summary>
    ///     JVM-side version of this object.
    /// </summary>
    [JavaIgnore] [JsonIgnore] public JavaClass InternalClass = null!;

    /// <summary>
    ///     Json helper to serialize/deserialize attached class. NEVER touch it. Use <see cref="JavaClass" /> to take object's
    ///     class.
    ///     Deserialization MUST occur withing JVM context.
    /// </summary>
    [JsonProperty]
    public string? InternalClassName
    {
        get => InternalClass != null! ? InternalClass.Name : null;
        set
        {
            if (value == null)
                return;
            InternalClass = Jvm.GetClass(value);
        }
    }

    [return: JavaType(typeof(Class))]
    public static Reference forName([String] Reference r)
    {
        var name = Jvm.ResolveString(r);
        if (!Jvm.Classes.TryGetValue(name.Replace('.', '/'), out var jc))
            Jvm.Throw<ClassNotFoundException>(name);
        var cls = Jvm.AllocateObject<Class>();
        cls.InternalClass = jc;
        return cls.This;
    }

    [return: String]
    public Reference getName()
    {
        var name = InternalClass.Name.Replace('/', '.');
        return Jvm.AllocateString(name);
    }

    [JavaDescriptor("()Ljava/lang/Object;")]
    public JavaMethodBody newInstance(JavaClass cls)
    {
        return new JavaMethodBody(2, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("allocate", "()Ljava/lang/Object;")).Split()),
                new(JavaOpcode.dup),
                new(JavaOpcode.invokevirtual, cls.PushConstant(new NameDescriptor("<init>", "()V")).Split()),
                new(JavaOpcode.areturn)
            }
        };
    }


    public Reference allocate()
    {
        if (!InternalClass.Methods.ContainsKey(new NameDescriptor("<init>", "()V")))
            Jvm.Throw<IllegalAccessException>();
        return Jvm.AllocateObject(InternalClass);
    }

    [return: JavaType(typeof(InputStream))]
    public Reference getResourceAsStream([String] Reference name)
    {
        var data = Jvm.GetResource(Jvm.ResolveString(name), InternalClass);
        if (data == null)
            return Reference.Null;

        var stream = Jvm.AllocateObject<ByteArrayInputStream>();
        var buf = Jvm.AllocateArray(data, "[B");
        stream.Init(buf);
        return stream.This;
    }

    public bool isInterface() => InternalClass.IsInterface;

    public bool isArray() => InternalClass.IsArray;

    public bool isInstance(Reference obj)
    {
        return Jvm.ResolveObject(obj).JavaClass.Is(InternalClass);
    }

    public bool isAssignableFrom([JavaType(typeof(Class))] Reference cls)
    {
        //TODO this does not take into account edge cases (like primitives)!
        var other = Jvm.Resolve<Class>(cls);
        return other.InternalClass.Is(InternalClass);
    }

    [return: String]
    public Reference toString()
    {
        var name = InternalClass.Name.Replace('/', '.');
        //TODO is primitive check
        //if (isPrimitive())
        //    return Jvm.AllocateString(name);
        //return Jvm.AllocateString((isInterface() ? "interface " : "class ") + name);
        return Jvm.AllocateString(name);
    }
}