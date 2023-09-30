using javax.microedition.rms;

namespace MahoTrans.Toolkit;

public interface IRecordStore
{
    string[] ListStores();

    /// <summary>
    /// Checks can the store be opened.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="createIfNotExists"></param>
    /// <returns>False if createIfNotExist=false and there is no such store. Throw <see cref="RecordStoreNotFoundException"/> in such case.</returns>
    bool OpenStore(string name, bool createIfNotExists);

    void CloseStore(string name);

    bool DeleteStore(string name);

    int AddRecord(string name, byte[] data, int offset, int count);

    void DeleteRecord(string name, int id);

    int GetSize(string name);

    byte[]? GetRecord(string name, int id);

    void SetRecord(string name, int id, byte[] data, int offset, int count);

    int GetNextId(string name);

    int GetCount(string name);
}