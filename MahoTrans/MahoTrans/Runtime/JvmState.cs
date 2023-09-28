using System.Reflection;
using javax.microedition.ams;
using MahoTrans.Loader;
using MahoTrans.Native;
using MahoTrans.Runtime.Types;
using MahoTrans.Toolkit;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

public class JvmState
{
    public IToolkit Toolkit;
    public JavaHeap Heap;
    public readonly Dictionary<string, JavaClass> Classes = new();

    /// <summary>
    /// List of all threads, attached to scheduler. Anyone can append threads here at any time.
    /// Threads must be removed only by themselves during execution, else behaviour is undefined.
    /// </summary>
    public readonly List<JavaThread> AliveThreads = new();

    /// <summary>
    /// Threads which are detached from scheduler. For example, waiting for object notify or timeout.
    /// </summary>
    public readonly Dictionary<int, JavaThread> WaitingThreads = new();

    private readonly object _threadPoolSwitchLock = new();

    private readonly Dictionary<string, byte[]> _resources = new();
    private readonly Dictionary<NameDescriptor, int> _virtualPointers = new();
    private int _virtualPointerRoller = 1;
    private bool _running;

    private EventQueue? _eventQueue;

    public JvmState(IToolkit toolkit)
    {
        Toolkit = toolkit;
        Heap = new(this);
    }

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

        InitClasses(@new);
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

    private void InitClasses(JavaClass[] classes)
    {
        RunInContext(() =>
        {
            foreach (var cls in classes)
            {
                if (cls.Methods.TryGetValue(new NameDescriptor("<clinit>", "()V"), out var init))
                {
                    if (init.IsNative)
                        init.NativeBody.Invoke(null, new object?[0]);
                    else
                        JavaThread.CreateSyntheticStaticAction(init, this).Execute(this);
                }
            }
        });
    }

    #endregion

    #region Calls

    public void RunInContext(Action action)
    {
        if (Object.HeapAttached)
            throw new JavaRuntimeError("This thread already has attached context!");

        try
        {
            Object.AttachHeap(Heap);
            action();
        }
        finally
        {
            Object.DetachHeap();
        }
    }

    public void RunInContextIfNot(Action action)
    {
        if (Object.HeapAttached)
        {
            action();
            return;
        }

        RunInContext(action);
    }

    public Method GetMethod(NameDescriptorClass descriptor, bool @static)
    {
        if (!Classes.TryGetValue(descriptor.ClassName, out var @class))
            throw new JavaRuntimeError($"Failed to get class {descriptor.ClassName}");
        try
        {
            return @class.Methods[descriptor.Descriptor];
        }
        catch (KeyNotFoundException)
        {
            throw new JavaRuntimeError(
                $"Failed to find {(@static ? "static" : "instance")} method {descriptor.Descriptor} in class {descriptor.ClassName}");
        }
    }

    public Method GetVirtualMethod(int pointer, Reference target)
    {
        var obj = Heap.ResolveObject(target);

        if (obj.JavaClass.VirtualTable!.TryGetValue(pointer, out var mt))
            return mt;

        throw new JavaRuntimeError("No virt method found");
    }

    #endregion

    #region Resources

    public sbyte[]? GetResource(string name)
    {
        if (name.StartsWith('/'))
        {
            if (_resources.TryGetValue(name.Substring(1), out var blob))
            {
                var copy = new sbyte[blob.Length];
                for (int i = 0; i < blob.Length; i++)
                {
                    //TODO
                    copy[i] = (sbyte)blob[i];
                }

                return copy;
            }

            return null;
        }
        else
        {
            //TODO
        }

        return null;
    }

    #endregion

    #region Threads management

    /// <summary>
    /// Registers a thread in this JVM.
    /// </summary>
    /// <param name="thread">Thread, ready to run.</param>
    public void RegisterThread(JavaThread? thread)
    {
        if (thread is null)
            throw new NullReferenceException("Attempt to register null thread.");

        Console.WriteLine("Thread registered!");
        AliveThreads.Add(thread);
    }

    public void Detach(JavaThread thread)
    {
        lock (_threadPoolSwitchLock)
        {
            if (!AliveThreads.Remove(thread))
                throw new JavaRuntimeError($"Attempt to detach {thread} which is not attached.");

            WaitingThreads.Add(thread.ThreadId, thread);
        }
    }

    /// <summary>
    /// Moves thread from waiting pool to active pool.
    /// </summary>
    /// <param name="id">Thread id to operate on.</param>
    /// <returns>False, if thread was not in waiting pool. Thread state is undefined in such state.</returns>
    public bool Attach(int id)
    {
        lock (_threadPoolSwitchLock)
        {
            if (WaitingThreads.Remove(id, out var th))
            {
                AliveThreads.Add(th);
                return true;
            }

            return false;
        }
    }

    #endregion

    /// <summary>
    /// Runs all registered threads in cycle. This method may never return.
    /// </summary>
    public void Execute()
    {
        RunInContext(() =>
        {
            _running = true;
            var count = AliveThreads.Count;
            while (_running)
            {
                var allAlive = true;

                for (int i = count - 1; i >= 0; i--)
                {
                    var thread = AliveThreads[i];
                    if (thread.ActiveFrame != null)
                        JavaRunner.Step(thread, this);
                    else
                        allAlive = false;
                }

                var newCount = AliveThreads.Count;
                if (allAlive || count != newCount)
                {
                    // new thread launched or nobody ended yet
                    // going one more cycle
                    count = newCount;
                    continue;
                }

                for (int i = count - 1; i >= 0; i--)
                {
                    if (AliveThreads[i].ActiveFrame == null)
                        AliveThreads.RemoveAt(i);
                }

                newCount = AliveThreads.Count;
                if (newCount == 0)
                    return;

                count = newCount;
            }
        });
    }

    public void Stop()
    {
        _running = false;
    }

    public EventQueue EventQueue
    {
        get
        {
            if (_eventQueue != null)
                return _eventQueue;
            RunInContextIfNot(() =>
            {
                _eventQueue = Heap.AllocateObject<EventQueue>();
                _eventQueue.Jvm = this;
                _eventQueue.start();
            });
            return _eventQueue!;
        }
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

    public void LogOpcodeStats()
    {
        var d = ClassLoader.CountOpcodes(Classes.Values);

        foreach (var kvp in d.Where(x => x.Value != 0).OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"{kvp.Key.ToString(),-16} {kvp.Value}");
        }

        Console.WriteLine();
        Console.WriteLine("Unused:");
        foreach (var kvp in d.Where(x => x.Value == 0))
        {
            Console.WriteLine($"{kvp.Key}");
        }
    }
}