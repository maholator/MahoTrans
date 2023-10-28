using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

/// <summary>
/// This class exposes tools to build CLR types from JVM types.
/// </summary>
/// <seealso cref="NativeLinker"/>
public static class ClassCompiler
{
    private static readonly Dictionary<Type, MethodInfo> StackPoppers = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PopInt))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PopLong))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PopFloat))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PopDouble))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PopBool))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PopByte))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PopChar))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PopShort))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PopReference))! }
    };

    public static readonly Dictionary<Type, MethodInfo> StackPushers = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PushInt))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PushLong))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PushFloat))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PushDouble))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PushBool))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PushByte))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PushChar))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PushShort))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PushReference))! },
    };

    public static void CompileTypes(Dictionary<string, JavaClass> loaded, IEnumerable<JavaClass> queued,
        string assemblyName, string moduleName)
    {
        var classes = queued as JavaClass[] ?? queued.ToArray();
        var name = new AssemblyName(assemblyName);
        CompileTypes(loaded, classes, name, moduleName);
    }

    public static void CompileTypes(Dictionary<string, JavaClass> loaded, JavaClass[] queued, AssemblyName assemblyName,
        string moduleName)
    {
        Dictionary<string, JavaClass> queuedDict = queued.ToDictionary(x => x.Name, x => x);
        Dictionary<JavaClass, CompilerCache> cache = queued.ToDictionary(x => x, x => new CompilerCache(x));
        var builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule(moduleName);

        int counter = 0;
        while (true)
        {
            bool ready = true;

            foreach (var linked in queued)
            {
                var c = cache[linked];
                if (c.Builder != null)
                    continue;

                if (c.SuperType == null)
                {
                    if (linked.Flags.HasFlag(ClassFlags.Interface))
                    {
                        c.SuperType = typeof(object);
                    }
                    else if (linked.Name == "java/lang/Object")
                    {
                        c.SuperType = typeof(object);
                    }
                    else if (loaded.TryGetValue(linked.SuperName, out var bit))
                    {
                        c.SuperType = bit.ClrType;
                    }
                    else if (queuedDict.TryGetValue(linked.SuperName, out var lt))
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
                        throw new JavaLinkageException(linked.Name + "'s super class is " + linked.SuperName +
                                                       ", which can't be found.");
                    }
                }

                var inters = linked.Interfaces;
                foreach (var inter in inters)
                {
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
                                throw new JavaLinkageException(linked.Name + " has interface " + inter +
                                                               ", which can't be found.");
                            }
                        }
                    }
                }

                var clrFlags = linked.ClrFlags;
                if (linked.Flags.HasFlag(ClassFlags.Interface))
                    c.SuperType = null;

                counter++;
                c.Number = counter;

                c.Builder = module.DefineType(linked.Name, clrFlags, c.SuperType,
                    c.Interfaces.Values.ToArray()!);

                loopEnd: ;
            }

            if (ready)
                break;
        }

        // linking is done, all cache entries have builders

        // fields
        var jsonPropCon =
            typeof(JsonPropertyAttribute).GetConstructor(BindingFlags.Public, Array.Empty<Type>())!;

        foreach (var rawClass in queued)
        {
            var c = cache[rawClass];
            foreach (var field in rawClass.Fields.Values)
            {
                object o = DescriptorUtils.ParseDescriptor(field.Descriptor.Descriptor);
                var t = o as Type ?? typeof(Reference);
                var f = c.Builder!.DefineField(GetFieldName(field.Descriptor, rawClass), t, ConvertFlags(field.Flags));
                var jab = new CustomAttributeBuilder(jsonPropCon, Array.Empty<object>());
                f.SetCustomAttribute(jab);
                BuildBridges(c.Builder!, f, field.Descriptor, rawClass);
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
                field.NativeField = type.GetField(GetFieldName(field.Descriptor, item.Key),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)!;
                field.GetValue = type.GetMethod(GetFieldGetterName(field.Descriptor, item.Key),
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                    .CreateDelegate<Action<Frame>>();
                field.SetValue = type.GetMethod(GetFieldSetterName(field.Descriptor, item.Key),
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!
                    .CreateDelegate<Action<Frame>>();
            }
        }
    }

    public static void BuildBridges(TypeBuilder typeBuilder, FieldInfo field, NameDescriptor name, JavaClass cls)
    {
        {
            var getter = DefineBridge(GetFieldGetterName(name, cls), typeBuilder);

            getter.Emit(OpCodes.Ldarg_0);
            // frame

            if (field.IsStatic)
            {
                getter.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                getter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // frame > heap
                getter.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                getter.Emit(OpCodes.Call, StackPoppers[typeof(Reference)]);
                // frame > heap > ref
                getter.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // frame > object
                getter.Emit(OpCodes.Ldfld, field);
            }

            // frame > value
            getter.Emit(OpCodes.Call, StackPushers[field.FieldType]);
            // -
            getter.Emit(OpCodes.Ret);
        }
        {
            var setter = DefineBridge(GetFieldSetterName(name, cls), typeBuilder);

            if (field.IsStatic)
            {
                setter.Emit(OpCodes.Ldarg_0);
                // frame
                setter.Emit(OpCodes.Call, StackPoppers[field.FieldType]);
                // value
                setter.Emit(OpCodes.Stsfld, field);
                // -
                setter.Emit(OpCodes.Ret);
            }
            else
            {
                var val = setter.DeclareLocal(field.FieldType);
                setter.Emit(OpCodes.Ldarg_0);
                // frame
                setter.Emit(OpCodes.Call, StackPoppers[field.FieldType]);
                // value
                setter.Emit(OpCodes.Stloc, val);
                // -
                setter.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // heap
                setter.Emit(OpCodes.Ldarg_0);
                // heap > frame
                setter.Emit(OpCodes.Call, StackPoppers[typeof(Reference)]);
                // heap > ref
                setter.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // target
                setter.Emit(OpCodes.Ldloc, val);
                // target > value
                setter.Emit(OpCodes.Stfld, field);
                // -
                setter.Emit(OpCodes.Ret);
            }
        }
    }

    private static ILGenerator DefineBridge(string name, TypeBuilder builder)
    {
        var method = builder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, null, new[] { typeof(Frame) });
        method.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        return method.GetILGenerator();
    }

    public static string GetFieldName(NameDescriptor descriptor, JavaClass cls) => $"{cls.Name}_{descriptor}";

    public static string GetFieldGetterName(NameDescriptor descriptor, JavaClass cls) =>
        GetFieldName(descriptor, cls) + "_bridge_get";

    public static string GetFieldSetterName(NameDescriptor descriptor, JavaClass cls) =>
        GetFieldName(descriptor, cls) + "_bridge_set";

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