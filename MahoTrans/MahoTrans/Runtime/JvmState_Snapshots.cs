using System.IO.Compression;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    /// Gets snapshot of this JVM.
    /// </summary>
    /// <returns>File to save.</returns>
    public byte[] Snapshot()
    {
        using var snapshotStream = new MemoryStream();

        using (var zip = new ZipArchive(snapshotStream, ZipArchiveMode.Create, true))
        {
            zip.AddTextEntry("cycle_number.txt", s => s.Write(CycleNumber));
            zip.AddTextEntry("classes.txt", s =>
            {
                foreach (var cls in Classes.Values)
                {
                    s.Write(cls.GetSnapshotHash());
                    s.Write(' ');
                    s.WriteLine(cls.Name);
                }
            });
            zip.AddTextEntry("virtp_table.json", s =>
            {
                var t = JsonConvert.SerializeObject(_virtualPointers);
                s.Write(t);
            });
            zip.AddTextEntry("threads/alive.json", s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(AliveThreads));
                s.Write(t);
            });
            zip.AddTextEntry("threads/waiting.json", s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(WaitingThreads.Values));
                s.Write(t);
            });
            zip.AddTextEntry("threads/queue.json", s =>
            {
                var t = JsonConvert.SerializeObject(SnapshotThreads(_wakeingUpQueue));
                s.Write(t);
            });
            zip.AddTextEntry("threads/hooks.json", s =>
            {
                var t = JsonConvert.SerializeObject(_wakeupHooks.ToArray());
                s.Write(t);
            });
            zip.AddTextEntry("heap/strings.json", s =>
            {
                var t = JsonConvert.SerializeObject(_internalizedStrings);
                s.Write(t);
            });
            zip.AddTextEntry("heap/next.txt", s => s.Write(_nextObjectId));
            zip.AddTextEntry("heap/heap.json", s =>
            {
                var t = JsonConvert.SerializeObject(_heap,new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    NullValueHandling = NullValueHandling.Include,
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    ReferenceLoopHandling = ReferenceLoopHandling.Error,
                });
                s.Write(t);
            });
        }

        {
            snapshotStream.Position = 0;
            var blob = snapshotStream.ToArray();
            return blob;
        }
    }

    public void RestoreFromSnapshot()
    {
    }

    private SnapshotedThread[] SnapshotThreads(IEnumerable<JavaThread> threads) =>
        threads.Select(x => new SnapshotedThread(x)).ToArray();

    private class SnapshotedThread
    {
        public int Id;
        public Reference Model;
        public SnapshotedFrame[] Frames;

        public SnapshotedThread(JavaThread jt)
        {
            Id = jt.ThreadId;
            Model = jt.Model;
            Frames = jt.CallStack.Take(jt.ActiveFrameIndex + 1).Select(x =>
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