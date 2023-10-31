using javax.microedition.ams.events;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace javax.microedition.ams;

/// <summary>
/// Special thread that always works in JVM's background and handles events sent from AMS to midlet.
/// </summary>
public class EventQueue : Thread
{
    [JavaIgnore] [JsonIgnore] private Queue<Reference> _events = new();
    [JavaIgnore] private object _lock = new();

    [JsonIgnore] public int Length => _events.Count;

    /// <summary>
    /// JVM this event queue working in. This is used to allow calling event queueing from anywhere.
    /// </summary>
    [JavaIgnore] [JsonIgnore] public JvmState OwningJvm = null!;

    [JavaIgnore] public Dictionary<int, bool> QueuedRepaints = new();

    /// <summary>
    /// For snapshots.
    /// </summary>
    public Reference[] Events
    {
        get
        {
            lock (_lock)
                return _events.ToArray();
        }
        set
        {
            lock (_lock)
                _events = new Queue<Reference>(value);
        }
    }

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var e in _events)
            queue.Enqueue(e);
        foreach (var qr in QueuedRepaints.Keys)
            queue.Enqueue(qr);
    }

    private bool IsRepaintPendingFor(Reference r)
    {
        if (QueuedRepaints.TryGetValue(r.Index, out var b))
            return b;
        return false;
    }

    [JavaIgnore]
    public void Enqueue<T>(Action<T> setup) where T : Event
    {
        lock (_lock)
        {
            var e = OwningJvm.AllocateObject<T>();
            setup.Invoke(e);
            if (e is RepaintEvent re)
            {
                if (IsRepaintPendingFor(re.Target))
                    return;
                QueuedRepaints[re.Target.Index] = true;
            }

            _events.Enqueue(e.This);

            //Console.WriteLine($"{e.JavaClass} is enqueued");
            OwningJvm.Attach(JavaThread.ThreadId);
        }
    }

    public void parkIfQueueEmpty()
    {
        lock (_lock)
        {
            if (_events.Count == 0)
            {
                // no more events
                OwningJvm.Detach(JavaThread, 0);
                // it will stay detached until Enqueue is called.
            }
            // if there are more events - do nothing
        }
    }

    [return: JavaType(typeof(Event))]
    public Reference dequeue()
    {
        lock (_lock)
        {
            if (_events.Count == 0)
                return Reference.Null;
            var e = _events.Dequeue();
            if (Object.Jvm.ResolveObject(e) is RepaintEvent re)
            {
                QueuedRepaints[re.Target.Index] = false;
            }

            //Console.WriteLine($"{Heap.ResolveObject(e).JavaClass} is dequeued, {_events.Count} more...");
            return e;
        }
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
    {
        var parkMethod = new NameDescriptorClass(nameof(parkIfQueueEmpty), "()V", typeof(EventQueue).ToJavaName());
        var dequeueMethod = new NameDescriptorClass(nameof(dequeue), $"()L{typeof(Event).ToJavaName()};",
            typeof(EventQueue).ToJavaName());
        var invokeMethod = new NameDescriptorClass("invoke", "()V", typeof(Event).ToJavaName());
        return new JavaMethodBody(2, 1)
        {
            RawCode = new[]
            {
                // checking are there any events
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokespecial, cls.PushConstant(parkMethod).Split()),
                // calling event
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorenter),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokespecial, cls.PushConstant(dequeueMethod).Split()),

                new Instruction(JavaOpcode.dup),
                new Instruction(JavaOpcode.ifnonnull, new byte[] { 0, 9 }),
                new Instruction(JavaOpcode.pop),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorexit),
                new Instruction(JavaOpcode.@goto, new byte[] { 0, 8 }),

                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(invokeMethod).Split()),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorexit),
                // loop
                new Instruction(JavaOpcode.@goto, (-25).Split())
            }
        };
    }

    public Reference TakePendingRepaint()
    {
        lock (_lock)
        {
            var list = _events.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var evRef = list[i];
                if (Object.Jvm.ResolveObject(evRef) is RepaintEvent re)
                {
                    QueuedRepaints[re.Target.Index] = false;
                    list.RemoveAt(i);
                    _events = new Queue<Reference>(list);
                    return evRef;
                }
            }

            return Reference.Null;
        }
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody serviceRepaints(JavaClass cls)
    {
        /*
         * if(repaints pending)
         *      take object from queue
         *      do now
         *      return
         * return
         */
        var taker = new NameDescriptorClass(nameof(TakePendingRepaint), "()Ljava/lang/Object;", typeof(EventQueue));
        var invokeMethod = new NameDescriptorClass("invoke", "()V", typeof(Event).ToJavaName());
        return new JavaMethodBody(2, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorenter),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(taker).Split()),
                new Instruction(JavaOpcode.dup),
                new Instruction(JavaOpcode.ifnonnull, new byte[] { 0, 7 }),
                new Instruction(JavaOpcode.pop),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorexit),
                new Instruction(JavaOpcode.@return),
                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(invokeMethod).Split()),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.monitorexit),
                new Instruction(JavaOpcode.@return),
            }
        };
    }
}