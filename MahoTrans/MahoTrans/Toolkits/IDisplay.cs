using javax.microedition.lcdui;
using MahoTrans.Runtime;

namespace MahoTrans.Toolkits;

public interface IDisplay : IToolkit
{
    DisplayableHandle Register(Displayable d);

    void SetCurrent(DisplayableHandle handle);
    void SetNullCurrent();

    DisplayableHandle? GetCurrent();

    int GetWidth(DisplayableHandle handle);

    int GetHeight(DisplayableHandle handle);

    DisplayableType GetType(DisplayableHandle handle);

    void SetFullscreen(DisplayableHandle handle, bool state);

    void SetTitle(DisplayableHandle handle, string title);

    void CommandsRefreshed(DisplayableHandle handle, List<Reference> commands);

    GraphicsHandle GetGraphics(DisplayableHandle handle);

    void Flush(DisplayableHandle handle);

    void Flush(DisplayableHandle handle, int x, int y, int width, int height);

    void Release(DisplayableHandle handle);
}