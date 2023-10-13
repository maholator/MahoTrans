using javax.microedition.ams.events;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Thread = java.lang.Thread;

namespace javax.microedition.ams;

public class EventQueue : Thread
{
    [JavaIgnore] private Queue<Reference> _events = new();
    [JavaIgnore] private object _lock = new();

    /// <summary>
    /// JVM this event queue working in. This is used to allow calling event queueing from anywhere.
    /// </summary>
    [JavaIgnore] public JvmState Jvm = null!;

    [JavaIgnore]
    public void Enqueue<T>(Action<T> setup) where T : Event
    {
        lock (_lock)
        {
            var e = Jvm.Heap.AllocateObject<T>();
            setup.Invoke(e);
            _events.Enqueue(e.This);
            Jvm.Attach(JavaThread.ThreadId);
        }
    }

    public void parkIfQueueEmpty()
    {
        lock (_lock)
        {
            if (_events.Count == 0)
            {
                // no more events
                Jvm.Detach(JavaThread, 0);
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
            return _events.Dequeue();
        }
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass cls)
    {
        var parkMethod = new NameDescriptorClass(nameof(parkIfQueueEmpty), "()V", typeof(EventQueue).ToJavaName());
        var dequeueMethod = new NameDescriptorClass(nameof(dequeue), $"()L{typeof(Event).ToJavaName()};",
            typeof(EventQueue).ToJavaName());
        var invokeMethod = new NameDescriptorClass("invoke", "()V", typeof(Event).ToJavaName());
        return new JavaMethodBody(1, 1)
        {
            RawCode = new[]
            {
                // checking are there any events
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokespecial, cls.PushConstant(parkMethod).Split()),
                // calling event
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokespecial, cls.PushConstant(dequeueMethod).Split()),
                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(invokeMethod).Split()),
                // loop
                new Instruction(JavaOpcode.@goto, (-11).Split())
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
                if (Heap.ResolveObject(evRef) is RepaintEvent re)
                {
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
                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(taker).Split()),
                new Instruction(JavaOpcode.dup),
                new Instruction(JavaOpcode.ifnonnull, new byte[] { 0, 5 }),
                new Instruction(JavaOpcode.pop),
                new Instruction(JavaOpcode.@return),
                new Instruction(JavaOpcode.invokevirtual, cls.PushConstant(invokeMethod).Split()),
                new Instruction(JavaOpcode.@return),
            }
        };
    }
}