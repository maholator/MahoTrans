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

    [JavaIgnore]
    public JavaMethodBody GenerateBridge(JavaClass cls, string callbackName)
    {
        var thisName = typeof(CanvasPointerEvent).ToJavaName();
        return new JavaMethodBody(3, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Canvas), thisName)).Split()),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("X", "I", thisName)).Split()),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Y", "I", thisName)).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor(callbackName, "(II)V")).Split()),
                new Instruction(JavaOpcode.@return)
            }
        };
    }
}