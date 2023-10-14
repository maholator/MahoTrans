using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public class CanvasKeyboardEvent : Event
{
    [JavaType(typeof(Canvas))] public Reference Target;
    public int Keycode;

    [InitMethod]
    public void Init([JavaType(typeof(Canvas))] Reference target, int keycode)
    {
        Target = target;
        Keycode = keycode;
    }

    [JavaIgnore]
    public JavaMethodBody GenerateBridge(JavaClass cls, string callbackName)
    {
        var thisName = typeof(CanvasPointerEvent).ToJavaName();
        return new JavaMethodBody(2, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Canvas), thisName)).Split()),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Keycode", "I", thisName)).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor(callbackName, "(I)V")).Split()),
                new Instruction(JavaOpcode.@return)
            }
        };
    }
}