using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkits;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace MahoTrans.Loader;

/// <summary>
/// This class exposes tools to build CLR types from JVM types.
/// </summary>
/// <seealso cref="NativeLinker"/>
public static class ClassCompiler
{
    public static void CompileTypes(Dictionary<string, JavaClass> loaded, JavaClass[] queued,
        string assemblyName, string moduleName, ILogger logger)
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
                        logger.PrintLoadTime(LogLevel.Error, cls.Name,
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
                                logger.PrintLoadTime(LogLevel.Error, cls.Name,
                                    $"The class has interface \"{inter}\" which can't be found. Dummy interface will be used instead.");
                                cls.Interfaces[i] = typeof(DummyInterface).ToJavaName();
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
        var jsonPropCon =
            typeof(JsonPropertyAttribute).GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                Array.Empty<Type>())!;

        foreach (var rawClass in queued)
        {
            var c = cache[rawClass];
            foreach (var field in rawClass.Fields.Values)
            {
                object o = DescriptorUtils.ParseDescriptor(field.Descriptor.Descriptor);
                var t = o as Type ?? typeof(Reference);
                var f = c.Builder!.DefineField(BridgeCompiler.GetFieldName(field.Descriptor, rawClass), t,
                    ConvertFlags(field.Flags));
                var jab = new CustomAttributeBuilder(jsonPropCon, Array.Empty<object>());
                f.SetCustomAttribute(jab);
                BridgeCompiler.BuildBridges(c.Builder!, f, field.Descriptor, rawClass);
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
                field.NativeField = type.GetField(BridgeCompiler.GetFieldName(field.Descriptor, item.Key),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)!;
                field.GetValue = type.GetMethod(BridgeCompiler.GetFieldGetterName(field.Descriptor, item.Key),
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                    .CreateDelegate<Action<Frame>>();
                field.SetValue = type.GetMethod(BridgeCompiler.GetFieldSetterName(field.Descriptor, item.Key),
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