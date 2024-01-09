// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

/// <summary>
///     This class exposes tools to build JVM types from CLR types.
/// </summary>
/// <seealso cref="ClassCompiler" />
/// <seealso cref="BridgeCompiler"/>
public static class NativeLinker
{
    private static int _bridgeAsmCounter = 1;

    public static JavaClass[] Make(Type[] types)
    {
        var name = new AssemblyName($"Bridge-{_bridgeAsmCounter}");
        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule($"Bridge-{_bridgeAsmCounter}");
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


        try
        {
            jc.Methods = javaMethods.ToDictionary(x => x.Descriptor, x => x);
        }
        catch (ArgumentException e)
        {
            throw new JavaLinkageException($"Dublicate method in class {name}", e);
        }

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
                throw new JavaLinkageException(
                    $"Java method builder must have a descriptor attribute - method {nativeName} in {clrType.FullName} doesn't.");

            var d = new NameDescriptor(name, descriptor);
            var target = nativeMethod.IsStatic ? null : Activator.CreateInstance(clrType);
            var body = (JavaMethodBody)nativeMethod.Invoke(target, new object[] { javaType })!;

            return new Method(d, flags, javaType)
            {
                JavaBody = body
            };
        }

        var ms = $"Initialization method {javaType.Name}::{nativeMethod.Name}";

        if (isCtor && isClinit)
            throw new JavaLinkageException(
                $"{ms} must be either instance or static.");
        if (isCtor && ret.Native != typeof(void))
            throw new JavaLinkageException(
                $"{ms} must return void.");
        if (isCtor && nativeMethod.IsStatic)
            throw new JavaLinkageException(
                $"{ms} can't be static.");
        if (isClinit && !nativeMethod.IsStatic)
            throw new JavaLinkageException(
                $"{ms} must be static.");
        if (isClinit && ret.Native != typeof(void))
            throw new JavaLinkageException(
                $"{ms} must return void.");

        descriptor ??= $"({string.Join("", args.Select(x => Parameter.FromParam(x).ToString()))}){ret.ToString()}";

        // building method

        flags |= MethodFlags.Native;
        Method java = new Method(new NameDescriptor(name, descriptor), flags, javaType);
        java.NativeBody = nativeMethod;
        try
        {
            java.BridgeNumber = BridgeCompiler.BuildBridgeMethod(nativeMethod, bridgeBuilder);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException($"Failed to build native bridges for {javaType.Name}::{nativeMethod.Name}",
                e);
        }

        return java;
    }


    /// <summary>
    /// Checks, should the field be shown to JVM.
    /// </summary>
    /// <param name="field">Field to check.</param>
    /// <returns>False to hide field.</returns>
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

        if (t.EnumerateBaseTypes().Contains(typeof(Object)))
            throw new JavaLinkageException(
                $"{field.DeclaringType} has field {field.Name} of type {t.Name} which is java type. To store java objects, {nameof(Reference)} structs must be used.");

        // usually used for internal vectors.
        if (t == typeof(List<Reference>))
            return false;

        return true;
    }

    private readonly struct Parameter
    {
        public readonly Type Native;
        public readonly string? Java;

        public Parameter(Type native, string? java)
        {
            Native = native;
            Java = java;
        }

        public override string ToString()
        {
            if (Java == null)
                return Native.ToJavaDescriptorNative();
            if (Java.StartsWith('['))
                return Java;
            if (Java.StartsWith('L') && Java.EndsWith(';'))
                return Java;

            return $"L{Java};";
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