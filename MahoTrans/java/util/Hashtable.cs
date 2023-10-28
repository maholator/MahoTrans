using java.lang;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace java.util;

public class Hashtable : Object
{
    [JavaIgnore] [JsonProperty] private Dictionary<int, Reference> _storage = new();

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var r in _storage.Keys)
            queue.Enqueue(r);
        foreach (var r in _storage.Values)
            queue.Enqueue(r);
    }

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
        if (key.IsNull)
            return Reference.Null;
        if (_storage.TryGetValue(key.Index, out var value))
            return value;

        //TODO this is unsafe and slow!

        var pointer = Jvm.GetVirtualPointer(new NameDescriptor("equals", "(Ljava/lang/Object;)Z"));
        var method = Jvm.GetVirtualMethod(pointer, key);
        if (method.Class.IsObject)
        {
            // default equality comparer checks reference equality. We already did that.
            return Reference.Null;
        }

        if (method.IsNative)
        {
            var res = Jvm.ResolveObject(key);
            var func = method.NativeBody.CreateDelegate<Func<Reference, bool>>(res);
            foreach (var kvp in _storage)
            {
                if (func.Invoke(kvp.Key))
                    return kvp.Value;
            }

            return Reference.Null;
        }

        var retFrame = new Frame(new JavaMethodBody(1, 0)
        {
            RawCode = new[] { new Instruction(JavaOpcode.@return) },
            Method = new Method(new NameDescriptor("", ""), MethodFlags.Static, new JavaClass())
        });
        var equalityChecker = new JavaThread(retFrame, Reference.Null);
        foreach (var kvp in _storage)
        {
            equalityChecker.ActiveFrameIndex = 0;
            var f = equalityChecker.Push(method.JavaBody);
            f.PushReference(key);
            f.PushReference(kvp.Key);
            equalityChecker.Execute();
            bool equal = equalityChecker.CallStack[0]!.Stack[0] != 0;
            if (equal)
                return kvp.Value;
        }

        return Reference.Null;
    }

    public Reference put(Reference key, Reference val)
    {
        if (key.IsNull || val.IsNull)
            Jvm.Throw<NullPointerException>();

        if (_storage.TryGetValue(key.Index, out var prev))
        {
            _storage[key.Index] = val;
            return prev;
        }

        _storage[key.Index] = val;
        return Reference.Null;
    }

    public Reference remove(Reference key)
    {
        if (_storage.Remove(key.Index, out var p))
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
        var e = Jvm.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Keys.Select(x => new Reference(x)).ToArray();
        return e.This;
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Jvm.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Values.ToArray();
        return e.This;
    }
}