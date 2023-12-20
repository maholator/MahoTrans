using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class ArrayEnumerator : Object, Enumeration
{
    [JavaIgnore] public Reference[] Value = null!;
    public int Index;

    public bool hasMoreElements()
    {
        return Index < Value.Length;
    }

    public Reference nextElement()
    {
        if (Index >= Value.Length)
            Jvm.Throw<NoSuchElementException>();
        var el = Value[Index];
        Index++;
        return el;
    }

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        foreach (var r in Value)
            queue.Enqueue(r);
    }
}