// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Abstractions;
using MahoTrans.Compiler;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace MahoTrans.Loader;

/// <summary>
///     This class exposes tools to build CLR types from JVM types.
/// </summary>
/// <seealso cref="NativeLinker" />
public static class ClassCompiler
{
    public static void CompileTypes(Dictionary<string, JavaClass> loaded, JavaClass[] queued,
        string assemblyName, string moduleName, JvmState jvm, ILoadLogger? logger)
    {
        Dictionary<string, JavaClass> queuedDict = queued.ToDictionary(x => x.Name, x => x);
        Dictionary<JavaClass, CompilerCache> cache = queued.ToDictionary(x => x, x => new CompilerCache(x));
        var builder =
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule(moduleName);

        int counter = 0;
        while (true)
        {
            bool ready = true;

            foreach (var cls in queued)
            {
                var c = cache[cls];
                if (c.Builder != null)
                    continue;

                if (c.SuperType == null)
                {
                    if (cls.Flags.HasFlag(ClassFlags.Interface))
                    {
                        c.SuperType = typeof(object);
                    }
                    else if (cls.Name == "java/lang/Object")
                    {
                        c.SuperType = typeof(object);
                    }
                    else if (loaded.TryGetValue(cls.SuperName, out var bit))
                    {
                        c.SuperType = bit.ClrType;
                    }
                    else if (queuedDict.TryGetValue(cls.SuperName, out var lt))
                    {
                        var superC = cache[lt];
                        if (superC.Builder == null)
                        {
                            ready = false;
                            continue;
                        }

                        c.SuperType = superC.Builder;
                    }
                    else
                    {
                        logger?.Log(LoadIssueType.MissingClassSuper, cls.Name,
                            $"The class has super \"{cls.SuperName}\" which can't be found. lang.Object will be set as super.");
                        cls.SuperName = "java/lang/Object";
                        ready = false;
                        continue;
                    }
                }

                for (var i = 0; i < cls.Interfaces.Length; i++)
                {
                    var inter = cls.Interfaces[i];
                    if (c.Interfaces[inter] == null)
                    {
                        if (loaded.TryGetValue(inter, out var bit))
                            c.Interfaces[inter] = bit.ClrType;
                        else
                        {
                            if (queuedDict.TryGetValue(inter, out var lt))
                            {
                                var interCache = cache[lt];
                                if (interCache.Builder == null)
                                {
                                    ready = false;
                                    goto loopEnd; // continue
                                }

                                c.Interfaces[inter] = interCache.Builder;
                            }
                            else
                            {
                                logger?.Log(LoadIssueType.MissingClassSuper, cls.Name,
                                    $"The class has interface \"{inter}\" which can't be found. Dummy interface will be used instead.");
                                cls.Interfaces[i] = typeof(DummyInterface).ToJavaName();
                                c.Interfaces.Remove(inter);
                                c.Interfaces.TryAdd(cls.Interfaces[i], null);
                                ready = false;
                                goto loopEnd; // continue
                            }
                        }
                    }
                }

                var clrFlags = cls.ClrFlags;
                if (cls.Flags.HasFlag(ClassFlags.Interface))
                    c.SuperType = null;

                counter++;
                c.Number = counter;

                c.Builder = module.DefineType(cls.Name, clrFlags, c.SuperType,
                    c.Interfaces.Values.ToArray()!);

                loopEnd: ;
            }

            if (ready)
                break;
        }

        // linking is done, all cache entries have builders

        // fields
        var jsonPropCon = JsonPropConstructor;

        foreach (var cls in queued)
        {
            var c = cache[cls];
            foreach (var field in cls.Fields.Values)
            {
                if (field.IsStatic)
                {
                    jvm.StaticFieldsOwners.Add(field);
                    continue;
                }

                // field define
                object fieldType = DescriptorUtils.ParseDescriptor(field.Descriptor.Descriptor);
                var t = fieldType as Type ?? typeof(Reference);
                var f = c.Builder!.DefineField(FieldBridgeCompiler.GetFieldName(field.Descriptor, cls.Name), t,
                    ConvertFlags(field.Flags));
                // attribute attachment
                {
                    var jab = new CustomAttributeBuilder(jsonPropCon, Array.Empty<object>());
                    f.SetCustomAttribute(jab);
                }
                // bridges
                FieldBridgeCompiler.BuildBridges(c.Builder!, f, field.Descriptor, cls);
                // verify
                {
                    if (fieldType is not Type)
                    {
                        var clsName = fieldType as string;
                        if (clsName == null)
                        {
                            if (fieldType is DescriptorUtils.ArrayOf ao)
                                clsName = ao.Type as string;
                        }

                        if (clsName != null)
                        {
                            if (!queuedDict.ContainsKey(clsName) && !loaded.ContainsKey(clsName))
                            {
                                logger?.Log(LoadIssueType.MissingClassField, cls.Name,
                                    $"There is a field of type {clsName} which can't be found");
                            }
                        }
                    }
                }
            }
        }

        // build

        var list = cache.OrderBy(x => x.Value.Number);
        foreach (var item in list)
        {
            var type = item.Value.Builder!.CreateType()!;
            item.Key.ClrType = type;
            foreach (var field in item.Key.Fields.Values)
            {
                // static fields are not managed by CLR
                if (field.IsStatic)
                    continue;
                field.NativeField = type.GetField(FieldBridgeCompiler.GetFieldName(field.Descriptor, item.Key.Name),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)!;
                field.GetValue = type.GetMethod(FieldBridgeCompiler.GetGetterName(field.Descriptor, item.Key.Name),
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                    .CreateDelegate<Action<Frame>>();
                field.SetValue = type.GetMethod(FieldBridgeCompiler.GetSetterName(field.Descriptor, item.Key.Name),
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                    .CreateDelegate<Action<Frame>>();
            }
        }
    }

    public static FieldAttributes ConvertFlags(FieldFlags flags)
    {
        FieldAttributes a = 0;

        // access
#if USE_REAL_FIELD_ATTRIBS
        if (flags.HasFlag(FieldFlags.Public))
            a |= FieldAttributes.Public;
        if (flags.HasFlag(FieldFlags.Private))
            a |= FieldAttributes.Private;
        if (flags.HasFlag(FieldFlags.Protected))
            a |= FieldAttributes.FamORAssem;
        if (a == 0)
            a = FieldAttributes.Assembly;
#else
        a |= FieldAttributes.Public;
#endif
        // mods
        if (flags.HasFlag(FieldFlags.Static))
            a |= FieldAttributes.Static;

        // finals won't be final
        // volatile is not impl TODO
        // trans/synth/enum don't exist here

        return a;
    }

    private static ConstructorInfo JsonPropConstructor
    {
        get
        {
            var t = typeof(JsonPropertyAttribute);
            return t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>())!;
        }
    }

    private class CompilerCache
    {
        public CompilerCache(JavaClass c)
        {
            Interfaces = c.Interfaces.ToDictionary(x => x, _ => (Type?)null);
        }

        public TypeBuilder? Builder;
        public Type? SuperType;
        public int Number;
        public readonly Dictionary<string, Type?> Interfaces;
    }
}
