using System.Reflection;
using MahoTrans.Loader;
using MahoTrans.Native;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    /// List of all loaded classes. Array classes are created only when needed, use <see cref="GetClass"/> to construct them.
    /// </summary>
    public readonly Dictionary<string, JavaClass> Classes = new();

    private readonly Dictionary<string, byte[]> _resources = new();
    private readonly Dictionary<NameDescriptor, int> _virtualPointers = new();
    private int _virtualPointerRoller = 1;

    #region Class loading

    public void AddJvmClasses((JavaClass[], Dictionary<string, byte[]>) data, string assemblyName, string moduleName)
    {
        foreach (var kvp in data.Item2)
            _resources.Add(kvp.Key, kvp.Value);

        AddJvmClasses(data.Item1, assemblyName, moduleName);
    }

    public void AddJvmClasses(JavaClass[] classes, string assemblyName, string moduleName)
    {
        ClassCompiler.CompileTypes(Classes, classes, assemblyName, moduleName);
        foreach (var cls in classes)
        {
            Classes.Add(cls.Name, cls);
        }

        RefreshState(classes);
    }

    public void AddClrClasses(IEnumerable<Type> types)
    {
        var classes = NativeLinker.Make(types.ToArray());
        foreach (var cls in classes)
        {
            cls.Flags |= ClassFlags.Public;
            Classes.Add(cls.Name, cls);
        }

        RefreshState(classes);
    }

    /// <summary>
    /// Call this when new classes are loaded into JVM. Otherwise, they will be left in semi-broken state.
    /// </summary>
    /// <param name="new"></param>
    private void RefreshState(JavaClass[] @new)
    {
        foreach (var @class in @new)
        {
            if (@class.IsObject)
                continue;
            @class.Super = Classes[@class.SuperName];
        }

        foreach (var @class in Classes.Values)
            @class.RegenerateVirtualTable(this);
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
    /// Gets class object from <see cref="Classes"/>. Automatically handles array types.
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
                    var itemClass = itemDescr.Substring(1, itemDescr.Length - 2);
                    if (!Classes.ContainsKey(itemClass))
                        throw new JavaRuntimeError(
                            $"Class {name} can't be created because items class {itemClass} is not loaded.");
                    break;
                }
            }

            JavaClass ac = new JavaClass
            {
                Name = name,
                Super = GetClass("java/lang/Object"),
            };
            ac.RegenerateVirtualTable(this);
            Classes.Add(name, ac);
            return ac;
        }

        throw new JavaRuntimeError($"Class {name} is not loaded!");
    }

    #endregion

    #region Calls

    public Method GetVirtualMethod(int pointer, Reference target)
    {
        var obj = ResolveObject(target);

        if (obj.JavaClass.VirtualTable!.TryGetValue(pointer, out var mt))
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

    public sbyte[]? GetResource(string name)
    {
        Console.WriteLine($"Looking for resource {name}...");
        if (name.StartsWith('/'))
        {
            name = name.Substring(1);
        }

        if (_resources.TryGetValue(name, out var blob))
        {
            var copy = new sbyte[blob.Length];
            for (int i = 0; i < blob.Length; i++)
            {
                //TODO
                copy[i] = (sbyte)blob[i];
            }

            Console.WriteLine($"Returning buffer of {blob.Length} bytes.");
            return copy;
        }

        Console.WriteLine("Returning null!");
        return null;
    }

    #endregion
}