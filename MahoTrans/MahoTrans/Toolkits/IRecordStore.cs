using javax.microedition.rms;

namespace MahoTrans.Toolkits;

public interface IRecordStore : IToolkit
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

    /// <summary>
    /// Deletes record store. MIDP-side code must validate the operation before calling this method.
    /// </summary>
    /// <param name="name">Name of store.</param>
    /// <returns>False, if there is no such store.</returns>
    bool DeleteStore(string name);

    int AddRecord(string name, sbyte[] data, int offset, int count);

    void DeleteRecord(string name, int id);

    int GetSize(string name);

    int? GetSize(string name, int id);

    sbyte[]? GetRecord(string name, int id);

    void SetRecord(string name, int id, sbyte[] data, int offset, int count);

    int GetNextId(string name);

    int GetCount(string name);
}