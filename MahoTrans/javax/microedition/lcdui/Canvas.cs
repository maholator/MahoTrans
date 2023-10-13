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

    [JavaDescriptor("()V")]
    public JavaMethodBody repaint(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 2,
            RawCode = new Instruction[]
            {
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.aload_0),
                new Instruction(JavaOpcode.invokespecial,
                    @class.PushConstant(new NameDescriptorClass("ObtainGraphics",
                            "()Ljavax/microedition/lcdui/Graphics;",
                            "javax/microedition/lcdui/Canvas"))
                        .Split()),
                new Instruction(JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("paint", "(Ljavax/microedition/lcdui/Graphics;)V",
                            "javax/microedition/lcdui/Canvas"))
                        .Split()),
                new Instruction(JavaOpcode.@return)
            }
        };
    }

    public void serviceRepaints()
    {
        //TODO events loop
    }

    public int getGameAction(int keyCode) => 0;

    //TODO all this
    public void setFullScreenMode(bool mode)
    {
    }
}