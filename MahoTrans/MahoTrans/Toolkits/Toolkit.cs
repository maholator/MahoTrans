namespace MahoTrans.Toolkits;

public sealed class Toolkit
{
    public readonly ISystem System;

    public readonly IClock Clock;

    public readonly IImageManager Images;

    public readonly IFontManager Fonts;

    public readonly IDisplay Display;

    public readonly IAms Ams;

    public readonly IRecordStore RecordStore;

    public Toolkit(ISystem system, IClock clock, IImageManager images, IFontManager fonts, IDisplay display, IAms ams, IRecordStore recordStore)
    {
        System = system;
        Clock = clock;
        Images = images;
        Fonts = fonts;
        Display = display;
        Ams = ams;
        RecordStore = recordStore;
    }
}