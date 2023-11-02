using System.IO.Compression;
using System.Text;
using javax.microedition.ams;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    public static readonly JsonSerializerSettings HeapSerializeSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        NullValueHandling = NullValueHandling.Include,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        Converters = { new CustomConvertorAssembly() },
        EqualityComparer = CustomJsonEqualityComparer.Instance
    };


    private const string cycle_number_txt = "cycle_number.txt";
    private const string classes_txt = "classes.txt";
    private const string virtp_table_json = "virtp_table.json";
    private const string threads_alive_json = "threads/alive.json";
    private const string threads_waiting_json = "threads/waiting.json";
    private const string threads_queue_json = "threads/queue.json";
    private const string threads_hooks_json = "threads/hooks.json";
    private const string heap_strings_json = "heap/strings.json";
    private const string heap_next_txt = "heap/next.txt";
    private const string heap_heap_json = "heap/heap.json";
    private const string heap_statics_json = "heap/statics.json";

    /// <summary>
    /// Gets snapshot of this JVM.
    /// </summary>
    /// <returns>File to save.</returns>
    public byte[] Snapshot()
    {
        using var snapshotStream = new MemoryStream();

        using (var zip = new ZipArchive(snapshotStream, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            zip.AddTextEntry(cycle_number_txt, s => s.Write(CycleNumber));
            zip.AddTextEntry(classes_txt, s =>
            {
                foreach (var cls in Classes.Values)
                {
                    s.Write(cls.GetSnapshotHash());
                    s.Write(' ');
                    s.WriteLine(cls.Name);
                }
            });
            zip.AddTextEntry(virtp_table_json, s =>
            {
                var t = JsonConvert.SerializeObject(_virtualPointers);
                s.Write(t);
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
                var t = JsonConvert.SerializeObject(_heap, HeapSerializeSettings);
                s.Write(t);
            });
            zip.AddTextEntry(heap_statics_json, s =>
            {
                var all = Classes.Values
                    .Where(x => !x.IsInterface && x.ClrType != null && !x.ClrType.IsAbstract)
                    .Select(x =>
                    {
                        var obj = (Object)Activator.CreateInstance(x.ClrType!);
                        obj.JavaClass = x;
                        return obj;
                    })
                    .ToArray();
                var t = JsonConvert.SerializeObject(all, HeapSerializeSettings);
                s.Write(t);
            });
        }

        {
            snapshotStream.Position = 0;
            var blob = snapshotStream.ToArray();
            return blob;
        }
    }

    public void RestoreFromSnapshot(Stream stream)
    {
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true, Encoding.UTF8))
        {
            _cycleNumber = long.Parse(zip.ReadTextEntry(cycle_number_txt));
            //TODO check classes
            //TODO check virttable

            // threads
            {
                AliveThreads.Clear();
                AliveThreads.AddRange(Restore(zip, threads_alive_json));
                foreach (var thread in AliveThreads)
                {
                    for (int i = 0; i <= thread.ActiveFrameIndex; i++)
                    {
                        thread.CallStack[i]!.Method.EnsureBytecodeLinked();
                    }
                }
                WaitingThreads.Clear();
                foreach (var thread in Restore(zip, threads_waiting_json))
                {
                    for (int i = 0; i <= thread.ActiveFrameIndex; i++)
                    {
                        thread.CallStack[i]!.Method.EnsureBytecodeLinked();
                    }

                    WaitingThreads.Add(thread.ThreadId, thread);
                }

                _wakeingUpQueue = new Queue<JavaThread>(Restore(zip, threads_queue_json));
                _wakeupHooks = JsonConvert.DeserializeObject<ThreadWakeupHook[]>(zip.ReadTextEntry(threads_hooks_json))!
                    .ToList();
            }

            // heap
            {
                _nextObjectId = int.Parse(zip.ReadTextEntry(heap_next_txt));
                _internalizedStrings =
                    JsonConvert.DeserializeObject<Dictionary<string, int>>(zip.ReadTextEntry(heap_strings_json))!;
                Object.AttachHeap(this);
                _heap = JsonConvert.DeserializeObject<Object[]>(zip.ReadTextEntry(heap_heap_json),
                    HeapSerializeSettings)!;
                JsonConvert.DeserializeObject<object[]>(zip.ReadTextEntry(heap_statics_json), HeapSerializeSettings);
                Object.DetachHeap();
            }
        }

        SyncHeapAfterRestore();
    }

    /// <summary>
    /// Called from <see cref="RestoreFromSnapshot"/>. Links things to each other.
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

    private SnapshotedThread[] SnapshotThreads(IEnumerable<JavaThread> threads) =>
        threads.Select(SnapshotedThread.Create).ToArray();

    private JavaThread[] Restore(SnapshotedThread[] snapshotedThreads)
    {
        return snapshotedThreads.Select(x =>
        {
            var jt = new JavaThread(x.Model, x.Id);
            List<Frame> frames = new();
            foreach (var sh in x.Frames)
            {
                var method = GetClass(sh.ClassName).Methods[sh.MethodDescriptor].JavaBody;
                frames.Add(new Frame(method)
                {
                    Pointer = sh.Pointer,
                    LocalVariables = sh.LocalVariables.ToArray(),
                    Stack = sh.Stack.ToArray(),
                    StackTypes = sh.StackTypes.ToArray(),
                    StackTop = sh.StackTop
                });
            }

            jt.CallStack = frames.ToArray();
            jt.ActiveFrameIndex = frames.Count - 1;
            jt.ActiveFrame = frames.Last();
            return jt;
        }).ToArray();
    }

    private JavaThread[] Restore(ZipArchive zip, string name)
    {
        return Restore(JsonConvert.DeserializeObject<SnapshotedThread[]>(zip.ReadTextEntry(name))!);
    }

    private class SnapshotedThread
    {
        public int Id;
        public Reference Model;
        public SnapshotedFrame[] Frames;

        public static SnapshotedThread Create(JavaThread jt)
        {
            var s = new SnapshotedThread();
            s.Id = jt.ThreadId;
            s.Model = jt.Model;
            s.Frames = jt.CallStack.Take(jt.ActiveFrameIndex + 1).Select(x =>
            {
                return new SnapshotedFrame
                {
                    Pointer = x.Pointer,
                    LocalVariables = x.LocalVariables.ToArray(),
                    Stack = x.Stack.ToArray(),
                    StackTypes = x.StackTypes.ToArray(),
                    StackTop = x.StackTop,
                    MethodDescriptor = x.Method.Method.Descriptor,
                    ClassName = x.Method.Method.Class.Name,
                };
            }).ToArray();
            return s;
        }
    }

    private class SnapshotedFrame
    {
        public int Pointer;
        public long[] LocalVariables;
        public long[] Stack;
        public PrimitiveType[] StackTypes;
        public int StackTop;
        public NameDescriptor MethodDescriptor;
        public string ClassName;
    }
}