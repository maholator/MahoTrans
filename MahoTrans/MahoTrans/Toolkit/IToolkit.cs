namespace MahoTrans.Toolkit;

public interface IToolkit
{
    IImage CreateImmutableImage(byte[] data);

    IImage CreateImmutableImage(int[] rgb, int w, int h, bool alpha);
    IImage CreateImmutableImage(IImage image);
    IImage CreateMutableImage(int width, int height);

    IDisplay Display { get; }

    IRecordStore RecordStore { get; }

    ISystem System { get; }
}