using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public class DisplayableShownEvent : Event
{
    [JavaType(typeof(Displayable))] public Reference Target;

    [InitMethod]
    public void Init([JavaType(typeof(Displayable))] Reference target) => Target = target;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Displayable).ToJavaName(),
                        typeof(DisplayableShownEvent).ToJavaName())).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("showNotify", "()V")).Split())
            }
        };
    }
}