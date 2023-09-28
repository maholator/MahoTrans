using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public abstract class CanvasPointerEvent : Event
{
    [JavaType(typeof(Canvas))] public Reference Target;
    public int X;
    public int Y;

    [InitMethod]
    public void Init([JavaType(typeof(Canvas))] Reference target, int x, int y)
    {
        Target = target;
        X = x;
        Y = y;
    }

    protected abstract string callbackName { get; }

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Canvas).ToJavaName(),
                        typeof(CanvasPointerEvent).ToJavaName())).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor(callbackName, "(II)V")).Split())
            }
        };
    }
}