using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

public interface IDisplay
{
    DisplayableDescriptor Register(Displayable d);

    DisplayableDescriptor? Current { get; set; }

    IDisplayable Resolve(DisplayableDescriptor descriptor);
}