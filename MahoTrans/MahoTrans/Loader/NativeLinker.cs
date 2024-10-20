// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MahoTrans.Abstractions;
using MahoTrans.Compiler;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Loader;

/// <summary>
///     This class exposes tools to build JVM types from CLR types.
/// </summary>
/// <seealso cref="ClassCompiler" />
/// <seealso cref="FieldBridgeCompiler" />
public static class NativeLinker
{
    private static int _bridgeAsmCounter = 1;

    /// <summary>
    ///     Converts list of CLR types to list of JVM types.
    /// </summary>
    /// <param name="types">Types to convert.</param>
    /// <param name="logger">Logger to log issues to.</param>
    /// <returns>List of java classes.</returns>
    public static List<JavaClass> Make(Type[] types, ILoadLogger? logger)
    {
        // bridges asm init
        var name = new AssemblyName($"{JvmState.NATIVE_BRIDGE_DLL_PREFIX}{_bridgeAsmCounter}");

        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule($"{JvmState.NATIVE_BRIDGE_DLL_PREFIX}{_bridgeAsmCounter}");
        var bridge = module.DefineType(CompilerUtils.BRIDGE_CLASS_NAME,
            TypeAttributes.Public | TypeAttributes.Sealed);
        _bridgeAsmCounter++;

        // building java classes
        List<JavaClass> java = new List<JavaClass>();
        foreach (var type in types)
            java.Add(Make(type, bridge, logger));

        // bridges compilation
        var loaded = bridge.CreateType()!;

        // linking bridges to java fields
        foreach (var @class in java)
        {
            foreach (var method in @class.Methods.Values)
            {
                if (method.BridgeNumber != 0)
                    method.Bridge = loaded.GetMethod($"bridge_{method.BridgeNumber}")!.CreateDelegate<Action<Frame>>();
            }

            foreach (var field in @class.Fields.Values)
            {
                field.GetValue = FieldBridgeCompiler.CaptureGetter(loaded, @class, field);
                field.SetValue = FieldBridgeCompiler.CaptureSetter(loaded, @class, field);
            }
        }


        return java;
    }

    private static JavaClass Make(Type type, TypeBuilder bridge, ILoadLogger? logger)
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
        var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly |
                                       BindingFlags.Instance | BindingFlags.Static);
        var nativeFields = allFields.Where(IsJavaVisible);
        var staticFields = StaticMemory.Fields.Where(x => x.Owner == type).Select(x => x.Field);
        var nativeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                            BindingFlags.Static);
        var interfaces = type.GetInterfaces();
        List<Method> javaMethods = new();
        foreach (var nm in nativeMethods)
        {
            if (nm.GetCustomAttribute<JavaIgnoreAttribute>() != null)
                continue;

            if (nm.IsSpecialName)
                continue;

            if (nm.Name == nameof(Object.AnnounceHiddenReferences))
                continue;

            if (nm.Name == nameof(Object.OnObjectDelete))
                continue;

            var built = BuildMethod(nm, jc, type, bridge, logger);
            javaMethods.Add(built);
        }

        List<string> javaInterfaces = new();
        foreach (var i in interfaces)
        {
            var ii = i.FullName!.Replace('.', '/');
            if (javaInterfaces.Contains(ii))
                continue;
            if (i.IsJavaType())
                javaInterfaces.Add(ii);
        }

        jc.Interfaces = javaInterfaces.ToArray();

        try
        {
            jc.Methods = javaMethods.ToDictionary(x => x.Descriptor, x => x);
        }
        catch (ArgumentException e)
        {
            throw new JavaLinkageException($"Duplicate method in class {name}", e);
        }

        jc.Fields = nativeFields.Concat(staticFields).Select(x =>
        {
            if (x.DeclaringType == typeof(StaticMemory))
            {
                var d = FieldBridgeCompiler.BuildNativeStaticBridge(bridge, x);
                var field = new Field(d, FieldFlags.Public | FieldFlags.Static, name);
                return field;
            }
            else
            {
                var d = new NameDescriptor(x.Name, Parameter.FromField(x).ToString());
                var flags = FieldFlags.Public;
                if (x.IsStatic)
                    flags |= FieldFlags.Static;

                var field = new Field(d, flags, name)
                {
                    NativeField = x,
                };
                FieldBridgeCompiler.BuildBridges(bridge, x, d, jc);
                return field;
            }
        }).ToDictionary(x => x.Descriptor, x => x);
        return jc;
    }

    private static Method BuildMethod(MethodInfo nativeMethod, JavaClass javaType, Type clrType,
        TypeBuilder bridgeBuilder, ILoadLogger? logger)
    {
        // collecting info

        var isCtor = nativeMethod.GetCustomAttribute<InitMethodAttribute>() != null;
        var isClinit = nativeMethod.GetCustomAttribute<ClassInitAttribute>() != null;
        var descriptor = nativeMethod.GetCustomAttribute<JavaDescriptorAttribute>()?.Descriptor;
        if (descriptor != null)
        {
            if (!descriptor.StartsWith('('))
                throw new JavaLinkageException(
                    $"Descriptor {descriptor} has no opening bracket! Check {clrType.FullName}.");
            if (descriptor.Count(x => x == ')') != 1)
                throw new JavaLinkageException(
                    $"Descriptor {descriptor} has invalid closing brackets! Check {clrType.FullName}.");
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
                throw new JavaLinkageException(
                    $"Java method builder can't build initialization method. There was an attempt in {clrType.FullName}.");

            if (args.Length != 1 || args[0].ParameterType != typeof(JavaClass))
                throw new JavaLinkageException(
                    $"Java method builder must take 1 argument - containing JVM type. Method {nativeName} in {clrType.FullName} doesn't.");

            if (descriptor == null)
                throw new JavaLinkageException(
                    $"Java method builder must have a descriptor attribute - method {nativeName} in {clrType.FullName} doesn't.");

            if (args[0].Name != "cls")
                logger?.Log(LoadIssueType.QuestionableNativeCode, clrType.ToJavaName(),
                    $"Method builder \"{nativeMethod.Name}\" argument should be named \"cls\", but it is named \"{args[0].Name}\".");

            var d = new NameDescriptor(name, descriptor);
            var target = nativeMethod.IsStatic ? null : Activator.CreateInstance(clrType);

            JavaMethodBody? body;
            try
            {
                body = nativeMethod.Invoke(target, new object[] { javaType })! as JavaMethodBody;
            }
            catch (Exception e)
            {
                throw new JavaLinkageException(
                    $"Method builder {nativeName} in {clrType.FullName} crashed.", e);
            }

            if (body == null)
                throw new JavaLinkageException(
                    $"Method builder {nativeName} in {clrType.FullName} returned invalid or null body.");

            return new Method(d, flags, javaType)
            {
                JavaBody = body
            };
        }

        var ms = $"Initialization method {javaType.Name}.{nativeMethod.Name}";

        if (isCtor && isClinit)
            throw new JavaLinkageException(
                $"{ms} must be either instance or static.");
        if (isCtor && !ret.IsVoid)
            throw new JavaLinkageException(
                $"{ms} must return void.");
        if (isCtor && nativeMethod.IsStatic)
            throw new JavaLinkageException(
                $"{ms} can't be static.");
        if (isClinit && !nativeMethod.IsStatic)
            throw new JavaLinkageException(
                $"{ms} must be static.");
        if (isClinit && !ret.IsVoid)
            throw new JavaLinkageException(
                $"{ms} must return void.");

        descriptor ??= $"({string.Join("", args.Select(x => Parameter.FromParam(x).ToString()))}){ret.ToString()}";

        // building method

        flags |= MethodFlags.Native;
        Method java = new Method(new NameDescriptor(name, descriptor), flags, javaType);
        java.NativeBody = nativeMethod;
        try
        {
            java.BridgeNumber = CallBridgeCompiler.BuildCallBridge(nativeMethod, bridgeBuilder);
        }
        catch (Exception e)
        {
            throw new JavaLinkageException($"Failed to build native bridges for {javaType.Name}.{nativeMethod.Name}",
                e);
        }

        return java;
    }

    /// <summary>
    ///     Checks, should the field be shown to JVM.
    /// </summary>
    /// <param name="field">Field to check.</param>
    /// <returns>False to hide field.</returns>
    public static bool IsJavaVisible(FieldInfo field)
    {
        // ignored fields are ignored.
        if (field.GetCustomAttribute<JavaIgnoreAttribute>() != null)
            return false;

        // service fields are ignored.
        if (field.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            return false;

        var t = field.FieldType;

        // enums are always service fields
        if (t.IsEnum)
            return false;

        // ND/NDC are service too
        if (t == typeof(NameDescriptor) || t == typeof(NameDescriptorClass))
            return false;

        // we use these internally - there is java.lang.Class and java.lang.String for JVM.
        if (t == typeof(JavaClass) || t == typeof(string))
            return false;

        // java objects must be stored by ref
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
        /// <summary>
        ///     Captures real type of parameter or field.
        /// </summary>
        private readonly Type _native;

        /// <summary>
        ///     Captures value of <see cref="JavaTypeAttribute" />.
        /// </summary>
        private readonly string? _java;

        private Parameter(Type native, string? java)
        {
            _native = native;
            _java = java;
        }

        public bool IsVoid => _native == typeof(void);

        public override string ToString()
        {
            if (!_native.IsArray)
                return toDescriptor(_native);

            // for arrays, we should add [[[[[ things ourselves.

            if (_native.GetArrayRank() != 1)
                throw new NotSupportedException("Multidimensional arrays are not supported");
            if (!_native.HasElementType) // no idea when it may happen but why not to check
                throw new NotSupportedException("Array must have element type");
            var udrType = _native.GetElementType()!;

            if (udrType.IsArray)
                throw new NotSupportedException("Nested arrays are not supported");

            // one more dimension
            return "[" + toDescriptor(udrType);
        }

        private string toDescriptor(Type t)
        {
            if (_java == null)
                return t.ToJavaDescriptor();

            if (_java.StartsWith('['))
                return _java;
            if (_java.StartsWith('L') && _java.EndsWith(';'))
                return _java;

            return $"L{_java};";
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
