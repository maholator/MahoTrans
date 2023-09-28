using javax.microedition.lcdui;
using MahoTrans.Toolkit;

namespace MahoTrans.Dummy.Toolkit;

public class DummyDisplay : IDisplay
{
    public int GetFontHeight(FontSize size)
    {
        return size switch
        {
            FontSize.Large => 18,
            FontSize.Medium => 14,
            FontSize.Small => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
        };
    }

    public int GetCharWidth(FontFace face, FontStyle style, int size, char c)
    {
        return 5;
    }

    public int GetCharsWidth(FontFace face, FontStyle style, int size, ReadOnlySpan<char> c)
    {
        return 5 * c.Length;
    }

    public void Register(Display d)
    {
    }

    public IDisplayable Register(Displayable d)
    {
        return new DummyDisplayable
        {
            Model = d
        };
    }

    public IDisplayable? Current { get; set; }
}