using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class Vector : Object
{
    [JavaIgnore] public List<Reference> List = null!;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        if (List == null!)
            return;
        foreach (var r in List) queue.Enqueue(r);
    }

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

    public void addElement(Reference r) => List.Add(r);

    public int capacity() => List.Capacity;

    public bool contains(Reference r) => List.Contains(r);

    public void copyInto([JavaType("[Ljava/lang/Object;")] Reference arr)
    {
        var a = Heap.ResolveArray<Reference>(arr);
        List.CopyTo(a);
    }

    public Reference elementAt(int i) => List[i];

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Heap.AllocateObject<ArrayEnumerator>();
        e.Value = List.ToArray();
        return e.This;
    }

    public void ensureCapacity(int min)
    {
    }

    public Reference firstElement()
    {
        if (List.Count == 0)
            Heap.Throw<NoSuchElementException>();
        return List[0];
    }

    public int size() => List.Count;

    public bool isEmpty() => List.Count == 0;

    public bool removeElement(Reference obj) => List.Remove(obj);

    public void removeAllElements() => List.Clear();

    public void removeElementAt(int index) => List.RemoveAt(index);

    public void setElementAt(Reference obj, int index)
    {
        if (index < 0 || index >= size())
            Heap.Throw<ArrayIndexOutOfBoundsException>();
        List[index] = obj;
    }

    public int indexOf(Reference obj)
    {
        //TODO equality
        return List.IndexOf(obj);
    }

    public void trimToSize()
    {
    }
}