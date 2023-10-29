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
        var g = Jvm.AllocateObject<Graphics>();
        g.Handle = Toolkit.Display.GetGraphics(Handle);
        return g.This;
    }

    public void repaint()
    {
        Jvm.EventQueue.Enqueue<RepaintEvent>(x => x.Target = This);
    }

    public void flushGraphics() => Toolkit.Display.Flush(Handle);

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

    public Reference getQueue() => Jvm.EventQueue.This;

    public int getGameAction(int keyCode)
    {
        switch (keyCode)
        {
            case -1:
            case '2':
                return 1;
            case -2:
            case '8':
                return 6;
            case -3:
            case '4':
                return 2;
            case -4:
            case '6':
                return 5;
            case -5:
            case 5:
                return 8;
            default:
                return 0;
        }
    }

    public bool hasPointerEvents() => true;

    public bool isShown() => true;

    public void setFullScreenMode(bool mode)
    {
        Toolkit.Display.SetFullscreen(Handle, mode);
    }

    #region Event stubs

    public void showNotify()
    {
    }

    public void hideNotify()
    {
    }

    public void pointerPressed(int x, int y)
    {
    }

    public void pointerDragged(int x, int y)
    {
    }

    public void pointerReleased(int x, int y)
    {
    }

    public void keyPressed(int k)
    {
    }

    public void keyReleased(int k)
    {
    }

    #endregion
}