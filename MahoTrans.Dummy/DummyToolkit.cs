using MahoTrans.Toolkit;

namespace MahoTrans.Dummy;

public class DummyToolkit : IToolkit
{
    public ISystem System => throw new NotImplementedException();

    public IClock Clock => throw new NotImplementedException();

    public IImageManager Images => throw new NotImplementedException();

    public IFontManager Fonts => throw new NotImplementedException();

    public IDisplay Display => throw new NotImplementedException();

    public IRecordStore RecordStore => throw new NotImplementedException();
}