namespace MahoTrans.Toolkit;

public interface IToolkit
{
    ISystem System { get; }

    IClock Clock { get; }

    IImageManager Images { get; }

    IFontManager Fonts { get; }

    IDisplay Display { get; }

    IAms Ams { get; }

    IRecordStore RecordStore { get; }
}