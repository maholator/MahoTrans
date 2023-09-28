using MahoTrans.Dummy.Toolkit;
using MahoTrans.Toolkit;

namespace MahoTrans.Dummy;

public class DummyToolkit : IToolkit
{
    public IImage CreateImmutableImage(byte[] data)
    {
        return new DummyImage();
    }

    public IImage CreateImmutableImage(int[] rgb, int w, int h, bool alpha)
    {
        return new DummyImage();
    }

    public IImage CreateImmutableImage(IImage image)
    {
        throw new NotImplementedException();
    }

    public IImage CreateMutableImage(int width, int height)
    {
        throw new NotImplementedException();
    }

    public IDisplay Display => new DummyDisplay();

    public IRecordStore RecordStore => new DummyRecordStore();

    public ISystem System => new DummySystem();
}