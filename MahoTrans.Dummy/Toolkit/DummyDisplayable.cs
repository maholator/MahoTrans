using javax.microedition.lcdui;
using MahoTrans.Toolkit;

namespace MahoTrans.Dummy.Toolkit;

public class DummyDisplayable : IDisplayable
{
    public Displayable Model { get; set; } = null!;
    public int Width { get; } = 640;
    public int Height { get; } = 360;
    public bool Fullscreen { get; set; }

    public IGraphics GetGraphics()
    {
        return new DummyGraphics();
    }

    public void Flush()
    {
    }
}