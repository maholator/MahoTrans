using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

public interface IDisplay
{
    DisplayableHandle Register(Displayable d);

    void SetCurrent(DisplayableHandle handle);
    void SetNullCurrent();

    int GetWidth(DisplayableHandle handle);

    int GetHeight(DisplayableHandle handle);

    DisplayableType GetType(DisplayableHandle handle);

    void SetFullscreen(DisplayableHandle handle, bool state);

    GraphicsHandle GetGraphics(DisplayableHandle handle);

    void Flush(DisplayableHandle handle);

    void Flush(DisplayableHandle handle, int x, int y, int width, int height);

    void Release(DisplayableHandle handle);
}