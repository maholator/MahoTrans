// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.ToolkitImpls.Rms;

/// <summary>
///     Record store implementation which holds all the data in a dictionary.
/// </summary>
public sealed class VirtualRms : ISnapshotableRecordStore
{
    /// <summary>
    ///     Keeps records. <br />
    ///     <b>Key:</b> store name in plain form. <br />
    ///     <b>Value:</b> zero-based list of records. Records are one-based so implementation must always do [index - 1].
    ///     Deleted records are null.
    /// </summary>
    private readonly Dictionary<string, List<byte[]?>> _storage;

    public IReadOnlyDictionary<string, List<byte[]?>> Storage => _storage;

    public VirtualRms() => _storage = new Dictionary<string, List<byte[]?>>();

    public VirtualRms(Dictionary<string, List<byte[]?>> data) => _storage = data;

    public string[] ListStores() => _storage.Keys.ToArray();

    public bool OpenStore(string name, bool createIfNotExists)
    {
        if (_storage.ContainsKey(name))
        {
            return true;
        }

        if (createIfNotExists)
        {
            _storage.Add(name, new List<byte[]?>());
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

    public int AddRecord(string name, ReadOnlySpan<byte> data)
    {
        var id = GetNextId(name);
        _storage[name][id] = data.ToArray();
        return id;
    }

    public bool DeleteRecord(string name, int id)
    {
        var store = _storage[name];

        if (id < 1 || id > store.Count)
            return false;

        if (store[id - 1] == null)
            return false;

        store[id - 1] = null;
        return true;
    }

    public int GetSize(string name)
    {
        int size = 0;
        foreach (var data in _storage[name])
        {
            if (data != null)
                size += data.Length;
        }

        return size;
    }

    public int? GetSize(string name, int id)
    {
        if (id < 1 || id > _storage[name].Count)
            return null;

        return _storage[name][id - 1]?.Length;
    }

    public int AvailableMemory => 1024 * 1024 * 1204;

    public byte[]? GetRecord(string name, int id)
    {
        if (id < 1 || id > _storage[name].Count)
            return null;
        return _storage[name][id - 1]?.ToArray();
    }

    public bool SetRecord(string name, int id, ReadOnlySpan<byte> data)
    {
        _storage[name][id - 1] = data.ToArray();
        return true;
    }

    public int GetNextId(string name)
    {
        // if store is empty, count is 0, our ID is 1.
        // if store has 2 records, 1 and 2, our ID is count+1 (2+1=3).
        return _storage[name].Count + 1;
    }

    public int GetCount(string name)
    {
        return _storage[name].Count(x => x != null);
    }

    public VirtualRms TakeSnapshot()
    {
        var dict = new Dictionary<string, List<byte[]?>>();

        foreach (var (name, slots) in _storage)
        {
            var clone = new List<byte[]?>(slots.Count);
            foreach (var slot in slots)
            {
                clone.Add(slot?.ToArray());
            }

            dict.Add(name, clone);
        }

        return new VirtualRms(dict);
    }
}