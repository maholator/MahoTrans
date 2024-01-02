// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public class RepaintEvent : Event
{
    [JavaType(typeof(Canvas))] public Reference Target;

    [JavaDescriptor("()V")]
    public JavaMethodBody invoke(JavaClass cls)
    {
        var thisName = typeof(RepaintEvent).ToJavaName();
        return new JavaMethodBody(3, 1)
        {
            RawCode = new[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.getfield,
                    cls.PushConstant(new NameDescriptorClass("Target", typeof(Canvas), thisName)).Split()),
                new Instruction(JavaOpcode.dup),
                new Instruction(JavaOpcode.dup),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("ObtainGraphics", "()Ljavax/microedition/lcdui/Graphics;"))
                        .Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("paint", "(Ljavax/microedition/lcdui/Graphics;)V")).Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor("flushGraphics", "()V")).Split()),
                new Instruction(JavaOpcode.@return)
            }
        };
    }
}