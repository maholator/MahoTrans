using javax.microedition.ams.events;
using MahoTrans.Native;
using MahoTrans.Runtime;

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