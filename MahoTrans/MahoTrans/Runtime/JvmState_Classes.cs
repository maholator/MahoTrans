// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using MahoTrans.Abstractions;
using MahoTrans.Compiler;
using MahoTrans.Loader;
using MahoTrans.Native;
using MahoTrans.Runtime.Errors;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    ///     List of all loaded classes. Array classes are created only when needed, use <see cref="GetClass" /> to construct
    ///     them.
    /// </summary>
    private readonly Dictionary<string, JavaClass> _classes = new();

    private readonly Dictionary<string, byte[]> _resources = new();
    private readonly Dictionary<NameDescriptor, int> _virtualPointers = new();
    private int _virtualPointerRoller = 1;

    public const string TYPE_HOST_DLL_PREFIX = "MTJvmTypesHost_";
    public const string NATIVE_BRIDGE_DLL_PREFIX = "MTBridgesHost_";
    public const string CROSS_ROUTINES_DLL_PREFIX = "MTCrossHost_";

    #region Class loading

    public void AddJvmClasses(JarPackage jar, string moduleName)
    {
        foreach (var kvp in jar.Resources)
            _resources.Add(kvp.Key, kvp.Value);

        AddJvmClasses(jar.Classes, moduleName);
    }

    public void AddJvmClasses(JavaClass[] classes, string moduleName)
    {
        if (_locked)
            throw new InvalidOperationException("Can't load classes in locked state.");

        using (new JvmContext(this))
        {
            ClassCompiler.CompileTypes(_classes, classes, $"{TYPE_HOST_DLL_PREFIX}{moduleName}", moduleName, this,
                Toolkit.LoadLogger);
            foreach (var cls in classes)
            {
                _classes.Add(cls.Name, cls);
            }

            refreshState(classes);
        }
    }

    public void AddClrClasses(IEnumerable<Type> types)
    {
        if (_locked)
            throw new InvalidOperationException("Can't load classes in locked state.");

        using (new JvmContext(this))
        {
            var classes = NativeLinker.Make(types.ToArray(), Toolkit.LoadLogger);
            foreach (var cls in classes)
            {
                cls.Flags |= ClassFlags.Public;
                _classes.Add(cls.Name, cls);
            }

            refreshState(classes);
        }
    }

    /// <summary>
    ///     Call this when new classes are loaded into JVM. Otherwise, they will be left in semi-broken state.
    /// </summary>
    /// <param name="new">Newly added classes.</param>
    private void refreshState(IEnumerable<JavaClass> @new)
    {
        foreach (var @class in @new)
        {
            if (@class.IsObject)
                continue;
            @class.Super = _classes[@class.SuperName];
        }

        foreach (var @class in _classes.Values)
        {
            @class.GenerateVirtualTable(this);
            @class.RecalculateSize();
        }

        if (StaticFields.Length < StaticFieldsOwners.Count)
        {
            var newStack = new long[StaticFieldsOwners.Count];
            Array.Copy(StaticFields, newStack, StaticFields.Length);
            StaticFields = newStack;
        }
    }

    public void AddClrClasses(Assembly assembly)
    {
        var all = assembly.GetTypes();
        var pending = all.Where(x => x.IsJavaType() && x.GetCustomAttribute<JavaIgnoreAttribute>() == null);
        AddClrClasses(pending);
    }

    /// <summary>
    ///     Imports MT assembly to this JVM.
    /// </summary>
    public void AddMahoTransLibrary() => AddClrClasses(typeof(JvmState).Assembly);

    /// <summary>
    ///     Links classes and locks this JVM.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void LinkAndLock()
    {
        if (_locked)
            throw new InvalidOperationException("JVM is already locked.");

        using (new JvmContext(this))
        {
            var classes = _classes.Values.ToList();
            for (var i = 0; i < classes.Count; i++)
            {
                var cls = classes[i];
                Toolkit.LoadLogger?.ReportLinkProgress(i, _classes.Count, cls.Name);
                BytecodeLinker.Link(cls);
            }
        }

        _locked = true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Unlock()
    {
        if (!_locked)
            throw new InvalidOperationException("JVM was not locked.");

        if (_running)
            throw new InvalidOperationException("Can't unlock running JVM.");

        foreach (var cls in _classes.Values)
        {
            foreach (var m in cls.Methods.Values)
            {
                m.JavaBody?.Clear();
            }
        }

        _locked = false;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CrossCompileLoaded()
    {
        if (!_locked)
            throw new InvalidOperationException("JVM was not locked.");
        using (new JvmContext(this))
        {
            CrossRoutineCompilerPass.CrossCompileAll(this);
        }
    }

    /// <summary>
    ///     Gets class object from <see cref="_classes" />. Automatically handles array types. Throws if no class found.
    /// </summary>
    /// <param name="name">Class name to search.</param>
    /// <returns>Found or created class object.</returns>
    public JavaClass GetClass(string name)
    {
        return GetClassOrNull(name) ?? throw new JavaRuntimeError($"Class {name} is not loaded!");
    }

    public bool IsClassLoaded(string name) => _classes.ContainsKey(name);

    public Dictionary<string, JavaClass>.ValueCollection LoadedClasses => _classes.Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLoadedClass(string name, [MaybeNullWhen(false)] out JavaClass cls) => _classes.TryGetValue(name, out cls);

    public JavaClass? GetLoadedClassOrNull(string name) => _classes.GetValueOrDefault(name);

    /// <summary>
    ///     Gets class object from <see cref="_classes" />. Automatically handles array types.
    /// </summary>
    /// <param name="name">Class name to search.</param>
    /// <returns>Class object if found or created, null otherwise.</returns>
    public JavaClass? GetClassOrNull(string name)
    {
        if (_classes.TryGetValue(name, out var o))
            return o;

        if (name.StartsWith('['))
        {
            // it's an array
            var itemDescr = name.Split('[', StringSplitOptions.RemoveEmptyEntries).Last();
            switch (itemDescr)
            {
                case "I":
                case "J":
                case "S":
                case "C":
                case "B":
                case "Z":
                case "F":
                case "D":
                    break;
                default:
                {
                    if (itemDescr[0] == 'L' && itemDescr[^1] == ';')
                    {
                        var itemClass = itemDescr.Substring(1, itemDescr.Length - 2);
                        if (!_classes.ContainsKey(itemClass))
                        {
                            if (name.Contains('.') && !name.Contains('/'))
                                throw new JavaRuntimeError(
                                    $"Items class {itemClass} is not loaded. It's suspicious that name contains dots and no slashed. Class name must be written using slashed.");

                            throw new JavaRuntimeError(
                                $"Class {name} can't be created because items class {itemClass} is not loaded.");
                        }
                    }
                    else
                    {
                        throw new JavaRuntimeError($"Malformed array descriptor: {name}");
                    }

                    break;
                }
            }

            JavaClass ac = new JavaClass
            {
                Name = name,
                Super = GetClass("java/lang/Object"),
            };
            ac.GenerateVirtualTable(this);
            _classes.Add(name, ac);
            return ac;
        }

        return null;
    }

    [Obsolete("Faulty method, see TODO", true)]
    //TODO: for java/lang/abc it returns... "[java/lang/abc"? No "L;"?
    public JavaClass WrapArray(JavaClass cls) => GetClass($"[{cls.Name}");

    #endregion

    #region Calls

    public Method GetVirtualMethod(int pointer, Reference target)
    {
        var obj = ResolveObject(target);

        if (obj.JavaClass.VirtualTableMap!.TryGetValue(pointer, out var mt))
            return mt;

        throw new JavaRuntimeError("No virt method found");
    }

    public int GetVirtualPointer(NameDescriptor nd)
    {
        lock (this)
        {
            if (_virtualPointers.TryGetValue(nd, out var i))
                return i;
            _virtualPointers.Add(nd, _virtualPointerRoller);
            i = _virtualPointerRoller;
            _virtualPointerRoller++;
            return i;
        }
    }

    public NameDescriptor DecodeVirtualPointer(int p)
    {
        return _virtualPointers.First(x => x.Value == p).Key;
    }

    #endregion

    #region Resources

    public sbyte[]? GetResource(string name, JavaClass? cls)
    {
        if (name.StartsWith('/'))
        {
            name = name.Substring(1);
        }
        else if (cls != null)
        {
            var li = cls.Name.LastIndexOf('/');
            if (li != -1)
            {
                name = $"{cls.Name.Substring(0, li + 1)}{name}";
            }
        }

        if (_resources.TryGetValue(name, out var blob))
        {
            Toolkit.Logger?.LogEvent(EventCategory.Resources,
                $"Resource {name} accessed, {blob.Length} bytes");
            var copy = blob.ConvertToSigned();
            return copy;
        }

        Toolkit.Logger?.LogEvent(EventCategory.Resources, $"Resource {name} not found");
        return null;
    }

    #endregion
}
