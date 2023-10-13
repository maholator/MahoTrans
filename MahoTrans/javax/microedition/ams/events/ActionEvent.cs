using java.lang;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public class ActionEvent : Event
{
    [JavaType(typeof(Runnable))] public Reference Target;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        var thisName = typeof(ActionEvent).ToJavaName();
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Runnable), thisName)).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("run", "()V")).Split()),
                new Instruction(JavaOpcode.@return)
            }
        };
    }
}