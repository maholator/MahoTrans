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

    [JavaIgnore]
    public void Enqueue<T>(Action<T> setup) where T : Event
    {
        lock (_lock)
        {
            var e = Heap.AllocateObject<T>();
            setup.Invoke(e);
            _events.Enqueue(e.This);
            Heap.State.Attach(JavaThread!.ThreadId);
        }
    }

    public void parkIfQueueEmpty()
    {
        lock (_lock)
        {
            if (_events.Count == 0)
            {
                // no more events
                Heap.State.Detach(JavaThread);
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
}