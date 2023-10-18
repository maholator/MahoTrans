using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

public interface IDisplay
{
    DisplayableHandle Register(Displayable d);

    DisplayableHandle? Current { get; set; }

    IDisplayable Resolve(DisplayableHandle handle);

    int GetWidth(DisplayableHandle handle);

    int GetHeight(DisplayableHandle handle);
}