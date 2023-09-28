using javax.microedition.lcdui;

namespace MahoTrans.Toolkit;

public interface IDisplay
{
    public int GetFontHeight(FontSize size);

    public int GetCharWidth(FontFace face, FontStyle style, int size, char c);

    public int GetCharsWidth(FontFace face, FontStyle style, int size, ReadOnlySpan<char> c);

    public void Register(Display d);

    public IDisplayable Register(Displayable d);
    
    public IDisplayable? Current { get; set; }
}