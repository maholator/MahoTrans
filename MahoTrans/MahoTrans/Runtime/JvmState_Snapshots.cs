// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO.Compression;
using System.Text;
using javax.microedition.ams;
using JetBrains.Annotations;
using MahoTrans.Runtime.Errors;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    private JsonSerializerSettings HeapJsonSettings => new()
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        NullValueHandling = NullValueHandling.Include,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        SerializationBinder = new Binder(this),
    };

    private const string cycle_number_txt = "cycle_number.txt";
    private const string classes_txt = "classes.txt";
    private const string threads_alive_json = "threads/alive.json";
    private const string threads_waiting_json = "threads/waiting.json";
    private const string threads_queue_json = "threads/queue.json";
    private const string threads_hooks_json = "threads/hooks.json";
    private const string heap_strings_json = "heap/strings.json";
    private const string heap_next_txt = "heap/next.txt";
    private const string heap_heap_json = "heap/heap.json";
    private const string heap_statics_json = "heap/statics.json";

    /// <summary>
    ///     Gets snapshot of this JVM.
    /// </summary>
    /// <returns>Stream with written snapshot. Position will be zero. Make sure do dispose it.</returns>
    [MustUseReturnValue]
    public MemoryStream Snapshot()
    {
        var snapshotStream = new MemoryStream();

        using (var zip = new ZipArchive(snapshotStream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            zip.AddTextEntry(cycle_number_txt, s => s.Write(CycleNumber));
            zip.AddTextEntry(classes_txt, s =>
            {
                foreach (var cls in Classes.Values)
                {
                    if (cls.IsArray)
                        continue;
                    s.Write(cls.PendingInitializer ? "1" : "0");
                    s.Write(' ');
                    s.Write(cls.ModelObject.Index);
                    s.Write(' ');
                    s.Write(cls.GetSnapshotHash());
                    s.Write(' ');
                    s.Write(cls.Name);
                    s.Write('\n');
                }
            });
            zip.AddTextEntry(threads_alive_json, s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(AliveThreads));
                s.Write(t);
            });
            zip.AddTextEntry(threads_waiting_json, s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(WaitingThreads.Values));
                s.Write(t);
            });
            zip.AddTextEntry(threads_queue_json, s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(_wakeingUpQueue));
                s.Write(t);
            });
            zip.AddTextEntry(threads_hooks_json, s =>
            {
                var t = JsonConvert.SerializeObject(_wakeupHooks.ToArray());
                s.Write(t);
            });
            zip.AddTextEntry(heap_strings_json, s =>
            {
                var t = JsonConvert.SerializeObject(_internalizedStrings);
                s.Write(t);
            });
            zip.AddTextEntry(heap_next_txt, s => s.Write(_nextObjectId));
            zip.AddTextEntry(heap_heap_json, s =>
            {
                var t = JsonConvert.SerializeObject(_heap, HeapJsonSettings);
                s.Write(t);
            });
            zip.AddTextEntry(heap_statics_json, s =>
            {
                SerializedStatics ss = default;
                ss.Java = StaticFields;
                ss.Native = StaticMemory;
                var t = JsonConvert.SerializeObject(ss, HeapJsonSettings);
                s.Write(t);
            });
        }

        snapshotStream.Position = 0;
        return snapshotStream;
    }

    private struct SerializedClass
    {
        public bool PendingInitializer;
        public int ModelObject;
        public string Hash;

        public SerializedClass(string[] line)
        {
            PendingInitializer = line[0][0] == '1';
            ModelObject = int.Parse(line[1]);
            Hash = line[2];
        }
    }

    public struct SerializedStatics
    {
        public StaticMemory Native;
        public long[] Java;
    }

    public void RestoreFromSnapshot(Stream stream)
    {
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true, Encoding.UTF8))
        {
            //classes
            {
                var classesList = zip.ReadTextEntry(classes_txt).Split('\n',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var classesDict = classesList
                    .Select(x => x.Split(' '))
                    .ToDictionary(x => x[3], x => new SerializedClass(x));

                foreach (var cls in Classes.Values)
                {
                    if (cls.IsArray)
                        continue;
                    if (!classesDict.TryGetValue(cls.Name, out var sn))
                    {
                        throw new SnapshotLoadError(
                            $"Class {cls.Name} isn't presented in snapshot. Probably, it was not loaded in previous run.");
                    }

                    var existingHash = cls.GetSnapshotHash().ToString();

                    if (existingHash != sn.Hash)
                    {
                        throw new SnapshotLoadError($"Class hash for {cls.Name} doesn't match snapshoted one.\n" +
                                                    $"Snapshoted hash: {sn.Hash}\n" +
                                                    $"Hash of existing class: {existingHash}\n" +
                                                    $"Class code or members may have changed.");
                    }

                    cls.PendingInitializer = sn.PendingInitializer;
                    cls.ModelObject = sn.ModelObject;
                    classesDict.Remove(cls.Name);
                }

                if (classesDict.Count != 0)
                {
                    throw new SnapshotLoadError(
                        $"Classes {string.Join(", ", classesDict.Keys)} are not loaded but present in snapshot.");
                }
            }

            _cycleNumber = long.Parse(zip.ReadTextEntry(cycle_number_txt));

            // threads
            {
                AliveThreads.Clear();
                AliveThreads.AddRange(Restore(zip, threads_alive_json));

                WaitingThreads.Clear();

                foreach (var thread in Restore(zip, threads_waiting_json))
                {
                    WaitingThreads.Add(thread.ThreadId, thread);
                }

                _wakeingUpQueue = new Queue<JavaThread>(Restore(zip, threads_queue_json));
                _wakeupHooks = JsonConvert.DeserializeObject<ThreadWakeupHook[]>(zip.ReadTextEntry(threads_hooks_json))!
                    .ToList();
            }

            // heap
            using (new JvmContext(this))
            {
                _nextObjectId = int.Parse(zip.ReadTextEntry(heap_next_txt));
                _internalizedStrings =
                    JsonConvert.DeserializeObject<Dictionary<string, int>>(zip.ReadTextEntry(heap_strings_json))!;


                _heap = DeserializeBound<Object[]>(zip.ReadTextEntry(heap_heap_json));
                var ss = DeserializeBound<SerializedStatics>(zip.ReadTextEntry(heap_statics_json));
                if (ss.Java.Length != StaticFieldsOwners.Count)
                    throw new SnapshotLoadError(
                        $"Statics count do not match. Expected {StaticFieldsOwners.Count}, {ss.Java.Length} was in snapshot.");
                StaticFields = ss.Java;
                StaticMemory = ss.Native;
            }
        }

        SyncHeapAfterRestore();
    }

    /// <summary>
    ///     Called from <see cref="RestoreFromSnapshot" />. Links things to each other.
    /// </summary>
    private void SyncHeapAfterRestore()
    {
        _eventQueue = _heap.OfType<EventQueue>().FirstOrDefault();
        if (_eventQueue != null)
            _eventQueue.OwningJvm = this;

        foreach (var thread in AliveThreads.Concat(WaitingThreads.Values).Concat(_wakeingUpQueue))
        {
            var t = (Thread)_heap[thread.Model.Index]!;
            t.JavaThread = thread;
        }
    }

    private static ThreadSnapshot[] SnapshotThreads(IEnumerable<JavaThread> threads) =>
        threads.Select(ThreadSnapshot.Create).ToArray();

    private JavaThread[] Restore(ThreadSnapshot[] threads)
    {
        var res = new JavaThread[threads.Length];
        for (int i = 0; i < threads.Length; i++)
        {
            res[i] = Restore(threads[i]);
        }

        return res;
    }

    private JavaThread Restore(ThreadSnapshot x)
    {
        var jt = new JavaThread(x.Model, x.Id);
        jt.WaitingForKill.AddRange(x.KillWaiters);
        List<Frame> frames = new();

        foreach (var sh in x.Frames)
        {
            var method = GetClass(sh.ClassName).Methods[sh.MethodDescriptor].JavaBody;
            var f = new Frame(method) { Pointer = sh.Pointer, StackTop = sh.StackTop };
            unsafe
            {
                fixed (long* ptr = sh.Stack)
                {
                    var len = sh.Stack.Length * sizeof(long);
                    Buffer.MemoryCopy(ptr, f.Stack, len, len);
                }

                fixed (long* ptr = sh.LocalVariables)
                {
                    var bytes = sh.LocalVariables.Length * sizeof(long);
                    Buffer.MemoryCopy(ptr, f.LocalVariables, bytes, bytes);
                }
            }

            frames.Add(f);
        }

        jt.CallStack = frames.ToArray();
        jt.ActiveFrameIndex = frames.Count - 1;
        jt.ActiveFrame = frames.Last();
        return jt;
    }

    private JavaThread[] Restore(ZipArchive zip, string name)
    {
        return Restore(JsonConvert.DeserializeObject<ThreadSnapshot[]>(zip.ReadTextEntry(name))!);
    }

    private class ThreadSnapshot
    {
        public int Id;
        public Reference Model;
        public int[] KillWaiters = Array.Empty<int>();
        public FrameSnapshot[] Frames = Array.Empty<FrameSnapshot>();

        public static ThreadSnapshot Create(JavaThread jt)
        {
            var s = new ThreadSnapshot();
            s.Id = jt.ThreadId;
            s.Model = jt.Model;
            s.KillWaiters = jt.WaitingForKill.ToArray();
            s.Frames = jt.CallStack.Take(jt.ActiveFrameIndex + 1).Select(x =>
            {
                var stack = x!.DumpStack();
                return new FrameSnapshot
                {
                    Pointer = x.Pointer,
                    LocalVariables = x.DumpLocalVariables(),
                    Stack = stack,
                    StackTop = x.StackTop,
                    MethodDescriptor = x.Method.Method.Descriptor,
                    ClassName = x.Method.Method.Class.Name,
                };
            }).ToArray();
            return s;
        }
    }

    private class FrameSnapshot
    {
        public int Pointer;
        public long[] LocalVariables = Array.Empty<long>();
        public long[] Stack = Array.Empty<long>();
        public int StackTop;
        public NameDescriptor MethodDescriptor;
        public string ClassName = string.Empty;
    }

    private class Binder : ISerializationBinder
    {
        private readonly JvmState _jvm;
        private readonly DefaultSerializationBinder _binder = new();

        public Binder(JvmState jvm)
        {
            _jvm = jvm;
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            if (assemblyName == null)
            {
                throw new JavaRuntimeError();
            }

            if (assemblyName.StartsWith(DYNAMIC_DLL_PREFIX))
            {
                return _jvm.Classes[typeName].ClrType ??
                       throw new JavaRuntimeError($"Can't bind to {typeName} because it has no CLR type");
            }

            return _binder.BindToType(assemblyName, typeName);
        }

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            _binder.BindToName(serializedType, out assemblyName, out typeName);
        }
    }

    private T DeserializeBound<T>(string json) => JsonConvert.DeserializeObject<T>(json, HeapJsonSettings)!;
}