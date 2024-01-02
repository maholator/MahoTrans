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
        get => InternalClass?.Name;
        set
        {
            if (value == null)
                return;
            InternalClass = Jvm.GetClass(value);
        }
    }

    private static readonly NameDescriptor _initDescr = new NameDescriptor("<init>", "()V");

    [return: JavaType(typeof(Class))]
    public static Reference forName([String] Reference r)
    {
        var name = Jvm.ResolveString(r);
        if (!Jvm.Classes.TryGetValue(name.Replace('.', '/'), out var jc))
            Jvm.Throw<ClassNotFoundException>();
        var cls = Jvm.AllocateObject<Class>();
        cls.InternalClass = jc!;
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
                new(JavaOpcode.invokevirtual, cls.PushConstant(_initDescr).Split()),
                new(JavaOpcode.areturn)
            }
        };
    }


    public Reference allocate()
    {
        if (!InternalClass.Methods.ContainsKey(_initDescr))
            Jvm.Throw<IllegalAccessException>();
        return Jvm.AllocateObject(InternalClass);
    }

    [return: JavaType(typeof(InputStream))]
    public Reference getResourceAsStream([String] Reference name)
    {
        var data = Jvm.GetResource(Jvm.ResolveString(name));
        if (data == null)
            return Reference.Null;

        var stream = Jvm.AllocateObject<ByteArrayInputStream>();
        var buf = Jvm.AllocateArray(data, "[B");
        stream.Init(buf);
        return stream.This;
    }
}