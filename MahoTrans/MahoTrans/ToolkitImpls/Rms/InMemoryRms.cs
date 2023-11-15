using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Rms;

/// <summary>
/// Record store implementation which holds all the data in a dictionary. There is no way to import/export the data.
/// </summary>
public class InMemoryRms : IRecordStore
{
    private readonly Dictionary<string, Dictionary<int, sbyte[]>> _storage = new();

    public string[] ListStores() => _storage.Keys.ToArray();

    public bool OpenStore(string name, bool createIfNotExists)
    {
        if (_storage.ContainsKey(name))
        {
            return true;
        }

        if (createIfNotExists)
        {
            _storage.Add(name, new Dictionary<int, sbyte[]>());
            return true;
        }

        return false;
    }

    public void CloseStore(string name)
    {
    }

    public bool DeleteStore(string name)
    {
        return _storage.Remove(name);
    }

    public int AddRecord(string name, sbyte[] data, int offset, int count)
    {
        var id = GetNextId(name);
        _storage[name][id] = data.Skip(offset).Take(count).ToArray();
        return id;
    }

    public void DeleteRecord(string name, int id)
    {
        throw new NotImplementedException();
    }

    public int GetSize(string name)
    {
        return 24;
    }

    public int? GetSize(string name, int id)
    {
        throw new NotImplementedException();
    }

    public sbyte[]? GetRecord(string name, int id)
    {
        if (_storage[name].TryGetValue(id, out var data))
            return data.ToArray();

        return null;
    }

    public void SetRecord(string name, int id, sbyte[] data, int offset, int count)
    {
        _storage[name][id] = data.Skip(offset).Take(count).ToArray();
    }

    public int GetNextId(string name)
    {
        var els = _storage[name].Keys.ToArray();
        if (els.Length == 0)
            return 1;

        return els.Max() + 1;
    }

    public int GetCount(string name)
    {
        return _storage[name].Count;
    }
}