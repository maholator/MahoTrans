// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using MahoTrans.Abstractions;
using MahoTrans.Loader;
using MahoTrans.Native;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    ///     List of all loaded classes. Array classes are created only when needed, use <see cref="GetClass" /> to construct
    ///     them.
    /// </summary>
    public readonly Dictionary<string, JavaClass> Classes = new();

    private readonly Dictionary<string, byte[]> _resources = new();
    private readonly Dictionary<NameDescriptor, int> _virtualPointers = new();
    private int _virtualPointerRoller = 1;

    #region Class loading

    public void AddJvmClasses(JarPackage jar, string assemblyName, string moduleName)
    {
        foreach (var kvp in jar.Resources)
            _resources.Add(kvp.Key, kvp.Value);

        AddJvmClasses(jar.Classes, assemblyName, moduleName);
    }

    public void AddJvmClasses(JavaClass[] classes, string assemblyName, string moduleName)
    {
        ClassCompiler.CompileTypes(Classes, classes, assemblyName, moduleName, this, Toolkit.LoadLogger);
        foreach (var cls in classes)
        {
            Classes.Add(cls.Name, cls);
        }

        RefreshState(classes);
        foreach (var cls in classes)
            BytecodeLinker.Verify(cls, this);
    }

    public void AddClrClasses(IEnumerable<Type> types)
    {
        var classes = NativeLinker.Make(types.ToArray(), Toolkit.LoadLogger);
        foreach (var cls in classes)
        {
            cls.Flags |= ClassFlags.Public;
            Classes.Add(cls.Name, cls);
        }

        RefreshState(classes);
        foreach (var cls in classes)
            BytecodeLinker.Verify(cls, this);
    }

    /// <summary>
    ///     Call this when new classes are loaded into JVM. Otherwise, they will be left in semi-broken state.
    /// </summary>
    /// <param name="new">Newly added classes.</param>
    private void RefreshState(IEnumerable<JavaClass> @new)
    {
        foreach (var @class in @new)
        {
            if (@class.IsObject)
                continue;
            @class.Super = Classes[@class.SuperName];
        }

        foreach (var @class in Classes.Values)
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
        var compatible = all.Where(x =>
        {
            return x.EnumerateBaseTypes().Contains(typeof(Object)) ||
                   x.GetCustomAttribute<JavaInterfaceAttribute>() != null;
        });
        var nonIgnored = compatible.Where(x => x.GetCustomAttribute<JavaIgnoreAttribute>() == null);
        AddClrClasses(nonIgnored);
    }

    /// <summary>
    ///     Gets class object from <see cref="Classes" />. Automatically handles array types.
    /// </summary>
    /// <param name="name">Class name to search.</param>
    /// <returns></returns>
    public JavaClass GetClass(string name)
    {
        if (Classes.TryGetValue(name, out var o))
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
                        if (!Classes.ContainsKey(itemClass))
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
            Classes.Add(name, ac);
            return ac;
        }

        throw new JavaRuntimeError($"Class {name} is not loaded!");
    }

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