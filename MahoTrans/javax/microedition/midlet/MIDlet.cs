using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.midlet;

public class MIDlet : Object
{
    [JavaIgnore] public Dictionary<string, string> Properties = new();

    public Reference Display;

    [InitMethod]
    public void Init()
    {
        //TODO
        Properties.Add("MIDlet-Version", "2.9.2");
        Properties.Add("Commit","abcdefgh");
    }

    [return: String]
    public Reference getAppProperty([String] Reference r)
    {
        string key = Heap.ResolveString(r);
        if (Properties.TryGetValue(key, out var val))
            return Heap.InternalizeString(val);
        return default;
    }
}