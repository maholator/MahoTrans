namespace MahoTrans.Toolkit;

public interface IRecordStore
{
    IRecordStoreEntry? Open(string name, bool createIfNotExists);

    void Delete(string name);
    
}