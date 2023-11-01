using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

/// <summary>
/// This class exposes tools to build JVM types from CLR types.
/// </summary>
/// <seealso cref="ClassCompiler"/>
public static class NativeLinker
{
    private static int _bridgeCounter = 1;

    private static readonly Dictionary<Type, MethodInfo> StackReversePoppers = new()
    {
        { typeof(int), typeof(Frame).GetMethod(nameof(Frame.PopIntFrom))! },
        { typeof(long), typeof(Frame).GetMethod(nameof(Frame.PopLongFrom))! },
        { typeof(float), typeof(Frame).GetMethod(nameof(Frame.PopFloatFrom))! },
        { typeof(double), typeof(Frame).GetMethod(nameof(Frame.PopDoubleFrom))! },
        { typeof(bool), typeof(Frame).GetMethod(nameof(Frame.PopBoolFrom))! },
        { typeof(sbyte), typeof(Frame).GetMethod(nameof(Frame.PopByteFrom))! },
        { typeof(char), typeof(Frame).GetMethod(nameof(Frame.PopCharFrom))! },
        { typeof(short), typeof(Frame).GetMethod(nameof(Frame.PopShortFrom))! },
        { typeof(Reference), typeof(Frame).GetMethod(nameof(Frame.PopReferenceFrom))! }
    };

    public static JavaClass[] Make(Type[] types)
    {
        var name = new AssemblyName($"Bridge-{_bridgeCounter}");
        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule($"Bridge-{_bridgeCounter}");
        var bridge = module.DefineType("Bridge", TypeAttributes.Public | TypeAttributes.Sealed);

        var java = types.Select(type => Make(type, bridge)).ToArray();

        var loaded = bridge.CreateType()!;

        foreach (var @class in java)
        {
            foreach (var method in @class.Methods.Values)
            {
                if (method.BridgeNumber != 0)
                    method.Bridge = loaded.GetMethod($"bridge_{method.BridgeNumber}")!.CreateDelegate<Action<Frame>>();
            }

            foreach (var field in @class.Fields.Values)
            {
                field.GetValue = loaded.GetMethod(BridgeCompiler.GetFieldGetterName(field.Descriptor, @class))!
                    .CreateDelegate<Action<Frame>>();
                field.SetValue = loaded.GetMethod(BridgeCompiler.GetFieldSetterName(field.Descriptor, @class))!
                    .CreateDelegate<Action<Frame>>();
            }
        }


        return java;
    }

    private static JavaClass Make(Type type, TypeBuilder bridge)
    {
        var name = type.FullName!.Replace('.', '/');
        JavaClass jc = new JavaClass
        {
            Name = name,
            ClrType = type,
            Flags = type.IsInterface ? (ClassFlags.Interface | ClassFlags.Public) : ClassFlags.Public
        };
        {
            var super = type.BaseType?.FullName?.Replace('.', '/');
            if (super != null)
                jc.SuperName = super;
        }
        var nativeFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                                          BindingFlags.Instance | BindingFlags.Static).Where(IsJavaVisible);
        var nativeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                            BindingFlags.Static);
        List<Method> javaMethods = new();
        foreach (var nm in nativeMethods)
        {
            if (nm.GetCustomAttribute<StaticFieldsAnnouncerAttribute>() != null)
            {
                if (nm.IsStatic)
                {
                    jc.StaticAnnouncer = nm.CreateDelegate<Action<List<Reference>>>();
                    continue;
                }

                throw new JavaLinkageException("Static announcer must be static!");
            }

            if (nm.GetCustomAttribute<JavaIgnoreAttribute>() != null)
                continue;

            if (nm.IsSpecialName)
                continue;

            if (nm.Name == nameof(Object.AnnounceHiddenReferences))
                continue;

            if (nm.Name == nameof(Object.OnObjectDelete))
                continue;

            var built = BuildMethod(nm, jc, type, bridge);
            javaMethods.Add(built);
        }


        jc.Methods = javaMethods.ToDictionary(x => x.Descriptor, x => x);
        jc.Fields = nativeFields.Select(x =>
        {
            var d = new NameDescriptor(x.Name, Parameter.FromField(x).ToString());
            FieldFlags flags = FieldFlags.Public;
            if (x.IsStatic)
                flags |= FieldFlags.Static;
            var field = new Field(d, flags)
            {
                NativeField = x,
            };
            BridgeCompiler.BuildBridges(bridge, x, d, jc);
            return field;
        }).ToDictionary(x => x.Descriptor, x => x);
        return jc;
    }

    private static Method BuildMethod(MethodInfo nativeMethod, JavaClass javaType, Type clrType,
        TypeBuilder bridgeBuilder)
    {
        // collecting info

        var isCtor = nativeMethod.GetCustomAttribute<InitMethodAttribute>() != null;
        var isClinit = nativeMethod.GetCustomAttribute<ClassInitAttribute>() != null;
        var descriptor = nativeMethod.GetCustomAttribute<JavaDescriptorAttribute>()?.Descriptor;
        if (descriptor != null)
        {
            if (!descriptor.StartsWith('('))
                throw new JavaLinkageException($"Descriptor {descriptor} has no opening bracket!");
            if (descriptor.Count(x => x == ')') != 1)
                throw new JavaLinkageException($"Descriptor {descriptor} has invalid closing brackets!");
        }

        var flags = MethodFlags.Public;
        if (nativeMethod.IsStatic)
            flags |= MethodFlags.Static;
        var nativeName = nativeMethod.Name.Split("___").First();
        var name = isCtor ? "<init>" : isClinit ? "<clinit>" : nativeName;
        var ret = Parameter.FromParam(nativeMethod.ReturnParameter);
        var args = nativeMethod.GetParameters();
        if (nativeMethod.ReturnParameter.ParameterType == typeof(JavaMethodBody))
        {
            if (isCtor || isClinit)
                throw new JavaLinkageException("Java method builder can't build initialization method.");

            if (args.Length != 1 || args[0].ParameterType != typeof(JavaClass))
                throw new JavaLinkageException("Java method builder must take 1 argument - containing JVM type.");

            if (descriptor == null)
                throw new JavaLinkageException("Java method builder must have a descriptor attribute.");

            var d = new NameDescriptor(name, descriptor);
            var target = nativeMethod.IsStatic ? null : Activator.CreateInstance(clrType);
            var body = (JavaMethodBody)nativeMethod.Invoke(target, new object[] { javaType })!;

            return new Method(d, flags, javaType)
            {
                JavaBody = body
            };
        }

        if (isCtor && isClinit)
            throw new JavaLinkageException("Initialization method must be either instance or static.");
        if (isCtor && ret.Native != typeof(void))
            throw new JavaLinkageException("Initialization method must return void.");
        if (isCtor && nativeMethod.IsStatic)
            throw new JavaLinkageException("Initialization method can't be static.");
        if (isClinit && !nativeMethod.IsStatic)
            throw new JavaLinkageException("Initialization method must be static.");
        if (isClinit && ret.Native != typeof(void))
            throw new JavaLinkageException("Initialization method must return void.");

        descriptor ??= $"({string.Join("", args.Select(x => Parameter.FromParam(x).ToString()))}){ret.ToString()}";

        // building method

        flags |= MethodFlags.Native;
        Method java = new Method(new NameDescriptor(name, descriptor), flags, javaType);
        java.NativeBody = nativeMethod;
        java.BridgeNumber = BuildBridgeMethod(nativeMethod, bridgeBuilder);
        return java;
    }

    private static int BuildBridgeMethod(MethodInfo method, TypeBuilder bridgeContainer)
    {
        int num = _bridgeCounter;
        _bridgeCounter++;
        var builder = bridgeContainer.DefineMethod($"bridge_{num}", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, null, new[] { typeof(Frame) });
        builder.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        var il = builder.GetILGenerator();
        var argsLength = method.GetParameters().Length;
        if (!method.IsStatic)
            argsLength++;

        // for push operation
        il.Emit(OpCodes.Ldarg_0);

        // all this is skipped if call is static and no args are needed.
        if (argsLength != 0)
        {
            // setting stack pointer for pops
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, argsLength);
            il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.SetFrom))!);


            if (!method.IsStatic)
            {
                // frame
                il.Emit(OpCodes.Call, typeof(Object).GetProperty(nameof(Object.Jvm))!.GetMethod!);
                // frame > heap
                il.Emit(OpCodes.Ldarg_0);
                // frame > heap > frame
                il.Emit(OpCodes.Call, StackReversePoppers[typeof(Reference)]);
                // frame > heap > ref
                il.Emit(OpCodes.Call, typeof(JvmState).GetMethod(nameof(JvmState.ResolveObject))!);
                // frame > object
            }

            foreach (var parameter in method.GetParameters())
            {
                il.Emit(OpCodes.Ldarg_0);
                var popper = StackReversePoppers[parameter.ParameterType];
                il.Emit(OpCodes.Call, popper);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, argsLength);
            il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.Discard))!);
        }

        il.Emit(OpCodes.Call, method);

        if (method.ReturnType != typeof(void))
        {
            // frame reference is here from the beginning
            il.Emit(OpCodes.Call, BridgeCompiler.StackPushers[method.ReturnType]);
        }
        else
        {
            il.Emit(OpCodes.Pop);
        }

        il.Emit(OpCodes.Ret);

        return num;
    }

    public static bool IsJavaVisible(FieldInfo field)
    {
        if (field.GetCustomAttribute<JavaIgnoreAttribute>() != null)
            return false;
        var t = field.FieldType;

        // enums are always service fields
        if (t.IsEnum)
            return false;

        // ND/NDC are service too
        if (t == typeof(NameDescriptor) || t == typeof(NameDescriptorClass))
            return false;

        // we use Class and String to show them to jvm
        if (t == typeof(JavaClass) || t == typeof(string))
            return false;

        return true;
    }

    private struct Parameter
    {
        public Type Native;
        public string? Java;

        public Parameter(Type native, string? java)
        {
            Native = native;
            Java = java;
        }

        public bool IsInt => Native == typeof(int) || Native == typeof(char) || Native == typeof(short) ||
                             Native == typeof(sbyte);

        public override string ToString()
        {
            if (Java != null)
            {
                if (Java.StartsWith('['))
                    return Java;
                return $"L{Java};";
            }

            if (Native == typeof(Reference))
                return "Ljava/lang/Object;";
            if (Native == typeof(int))
                return "I";
            if (Native == typeof(long))
                return "J";
            if (Native == typeof(float))
                return "F";
            if (Native == typeof(double))
                return "D";
            if (Native == typeof(char))
                return "C";
            if (Native == typeof(short))
                return "S";
            if (Native == typeof(sbyte))
                return "B";
            if (Native == typeof(bool))
                return "Z";
            if (Native == typeof(string))
                return "Ljava/lang/String;";
            if (Native == typeof(void))
                return "V";
            throw new ArgumentOutOfRangeException(nameof(Native), Native.ToString());
        }

        public static Parameter FromParam(ParameterInfo info)
        {
            string? java = info.GetCustomAttribute<JavaTypeAttribute>()?.Name;
            if (info.GetCustomAttribute<StringAttribute>() != null)
                java = "java/lang/String";
            Type t = info.ParameterType;
            return new Parameter(t, java);
        }

        public static Parameter FromField(FieldInfo info)
        {
            string? java = info.GetCustomAttribute<JavaTypeAttribute>()?.Name;
            if (info.GetCustomAttribute<StringAttribute>() != null)
                java = "java/lang/String";
            Type t = info.FieldType;
            return new Parameter(t, java);
        }
    }
}