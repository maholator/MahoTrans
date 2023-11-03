namespace MahoTrans.Toolkits;

/// <summary>
/// Object which holds all implementations needed for JVM to work.
/// </summary>
public sealed class Toolkit
{
    public readonly ISystem System;

    public readonly IClock Clock;

    public readonly IImageManager Images;

    public readonly IFontManager Fonts;

    public readonly IDisplay Display;

    public readonly IAms Ams;

    public readonly IRecordStore RecordStore;

    public ILogger Logger;

    public Toolkit(ISystem system, IClock clock, IImageManager images, IFontManager fonts, IDisplay display, IAms ams,
        IRecordStore recordStore, ILogger logger)
    {
        System = system;
        Clock = clock;
        Images = images;
        Fonts = fonts;
        Display = display;
        Ams = ams;
        RecordStore = recordStore;
        Logger = logger;
    }
}