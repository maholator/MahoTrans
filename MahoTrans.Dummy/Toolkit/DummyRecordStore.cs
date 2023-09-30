using MahoTrans.Toolkit;

namespace MahoTrans.Dummy.Toolkit;

public class DummyRecordStore : IRecordStore
{
    public string[] ListStores() => Array.Empty<string>();

    public bool OpenStore(string name, bool createIfNotExists) => false;

    public void CloseStore(string name)
    {
        throw new NotImplementedException();
    }

    public bool DeleteStore(string name)
    {
        throw new NotImplementedException();
    }

    public int AddRecord(string name, sbyte[] data, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public void DeleteRecord(string name, int id)
    {
        throw new NotImplementedException();
    }

    public int GetSize(string name)
    {
        throw new NotImplementedException();
    }

    public int? GetSize(string name, int id)
    {
        throw new NotImplementedException();
    }

    public sbyte[]? GetRecord(string name, int id)
    {
        throw new NotImplementedException();
    }

    public void SetRecord(string name, int id, sbyte[] data, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public int GetNextId(string name)
    {
        throw new NotImplementedException();
    }

    public int GetCount(string name)
    {
        throw new NotImplementedException();
    }
}