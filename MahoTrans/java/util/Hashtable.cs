using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class Hashtable : Object
{
    [JavaIgnore] private Dictionary<Reference, Reference> _storage = new();

    [InitMethod]
    public void Init()
    {
    }

    [InitMethod]
    public void Init(int cap)
    {
    }

    public void clear() => _storage.Clear();

    public Reference get(Reference key)
    {
        if (_storage.TryGetValue(key, out var value))
            return value;
        return Reference.Null;
    }

    public Reference put(Reference key, Reference val)
    {
        if (key.IsNull || val.IsNull)
            Heap.Throw<NullPointerException>();
        if (_storage.TryGetValue(key, out var prev))
        {
            _storage[key] = val;
            return prev;
        }

        _storage[key] = val;
        return Reference.Null;
    }

    public Reference remove(Reference key)
    {
        if (_storage.Remove(key, out var p))
            return p;
        return Reference.Null;
    }

    public int size() => _storage.Count;
    public bool isEmpty() => _storage.Count != 0;

    public void rehash()
    {
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference keys()
    {
        var e = Heap.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Keys.ToArray();
        return e.This;
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Heap.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Values.ToArray();
        return e.This;
    }
}