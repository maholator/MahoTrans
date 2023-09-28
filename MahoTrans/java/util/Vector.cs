using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class Vector : Object
{
    [JavaIgnore] public List<Reference> List = null!;

    [InitMethod]
    public new void Init()
    {
        List = new List<Reference>();
    }

    [InitMethod]
    public void Init(int capacity)
    {
        List = new List<Reference>(capacity);
    }

    [InitMethod]
    public void Init(int capacity, int incr)
    {
        List = new List<Reference>(capacity);
    }

    public Reference elementAt(int i) => List[i];

    public void addElement(Reference r) => List.Add(r);

    public int capacity() => List.Capacity;

    public int size() => List.Count;

    public bool isEmpty() => List.Count == 0;

    public void copyInto([JavaType("[Ljava/lang/Object;")] Reference arr)
    {
        var a = Heap.ResolveArray<Reference>(arr);
        List.CopyTo(a);
    }

    public void removeAllElements() => List.Clear();

    public void trimToSize()
    {
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Heap.AllocateObject<ArrayEnumerator>();
        e.Value = List.ToArray();
        return e.This;
    }
}