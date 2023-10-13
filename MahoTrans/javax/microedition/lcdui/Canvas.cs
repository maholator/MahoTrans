using javax.microedition.ams;
using javax.microedition.ams.events;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.lcdui;

public class Canvas : Displayable
{
    [InitMethod]
    public override void Init()
    {
        base.Init();
    }

    public void paint([JavaType(typeof(Graphics))] Reference g)
    {
    }

    [return: JavaType(typeof(Graphics))]
    public Reference ObtainGraphics()
    {
        var g = Heap.AllocateObject<Graphics>();
        g.Implementation = Handle.GetGraphics();
        return g.This;
    }

    public void repaint()
    {
        Heap.State.EventQueue.Enqueue<RepaintEvent>(x => x.Target = This);
    }

    public void flushGraphics() => Handle.Flush();

    [JavaDescriptor("()V")]
    public JavaMethodBody serviceRepaints(JavaClass cls)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokespecial,
                    cls.PushConstant(new NameDescriptorClass(nameof(getQueue), "()Ljava/lang/Object;", typeof(Canvas)))
                        .Split()),
                new(JavaOpcode.invokespecial,
                    cls.PushConstant(new NameDescriptorClass(nameof(EventQueue.serviceRepaints), "()V",
                            typeof(EventQueue)))
                        .Split()),
                new(JavaOpcode.@return),
            }
        };
    }

    public Reference getQueue() => Heap.State.EventQueue.This;

    public int getGameAction(int keyCode) => 0;

    //TODO all this
    public void setFullScreenMode(bool mode)
    {
    }
}