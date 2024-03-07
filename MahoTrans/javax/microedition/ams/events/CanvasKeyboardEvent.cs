// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using javax.microedition.lcdui;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.ams.events;

public class CanvasKeyboardEvent : Event
{
    [JavaType(typeof(Canvas))]
    public Reference Target;

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
        var thisName = typeof(CanvasKeyboardEvent).ToJavaName();
        return new JavaMethodBody(2, 1)
        {
            RawCode = new[]
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
