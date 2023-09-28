using MahoTrans.Toolkit;

namespace MahoTrans.Dummy.Toolkit;

public class DummyRecordStore : IRecordStore
{
    public IRecordStoreEntry? Open(string name, bool createIfNotExists)
    {
        return null;
    }

    public void Delete(string name)
    {
    }
}