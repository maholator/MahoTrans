// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.ToolkitImpls.Rms;

/// <summary>
///     Record store implementation which holds all the data in a dictionary.
/// </summary>
public class VirtualRms : IRecordStore
{
    /// <summary>
    ///     Keeps records. <br />
    ///     <b>Key:</b> store name in plain form. <br />
    ///     <b>Value:</b> zero-based list of records. Records are one-based so implementation must always do [index - 1]. Deleted records are null.
    /// </summary>
    private readonly Dictionary<string, List<sbyte[]?>> _storage = new();

    public string[] ListStores() => _storage.Keys.ToArray();

    public bool OpenStore(string name, bool createIfNotExists)
    {
        if (_storage.ContainsKey(name))
        {
            return true;
        }

        if (createIfNotExists)
        {
            _storage.Add(name, new List<sbyte[]?>());
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

    public int AddRecord(string name, ReadOnlySpan<sbyte> data)
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

    public sbyte[]? GetRecord(string name, int id)
    {
        if (id < 1 || id > _storage[name].Count)
            return null;
        return _storage[name][id - 1]?.ToArray();
    }

    public bool SetRecord(string name, int id, ReadOnlySpan<sbyte> data)
    {
        _storage[name][id - 1] = data.ToArray();
        return true;
    }

    public int GetNextId(string name)
    {
        return _storage[name].Count;
    }

    public int GetCount(string name)
    {
        return _storage[name].Count(x => x != null);
    }
}