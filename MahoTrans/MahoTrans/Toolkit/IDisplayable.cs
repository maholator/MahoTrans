using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

public interface IDisplayable
{
    public Displayable Model { get; }
    public int Width { get; }
    public int Height { get; }
    public bool Fullscreen { get; set; }
    public GraphicsHandle GetGraphics();
    void Flush();
}