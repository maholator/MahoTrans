using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui.game;

public class GameCanvas : Canvas
{
    [InitMethod]
    public void Init(bool events)
    {
        //TODO
        base.Init();
    }

    [return: JavaType(typeof(Graphics))]
    public Reference getGraphics()
    {
        return ObtainGraphics();
    }

    public void flushGraphics(int x, int y, int width, int height)
    {
        //TODO
        flushGraphics();
    }

    public int getKeyStates() => 0;
}