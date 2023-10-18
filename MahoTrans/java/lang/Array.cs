using MahoTrans.Native;
using MahoTrans.Runtime;
using ClrArray = System.Array;

namespace java.lang;

[JavaIgnore]
public class Array<T> : Array where T : struct
{
    [JavaIgnore] public T[] Value = null!;

    public override ClrArray BaseValue => Value;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        if (typeof(T) == typeof(Reference))
            Push((Reference[])(object)Value, queue);
    }

    private static void Push(Reference[] refs, Queue<Reference> q)
    {
        foreach (var r in refs)
            q.Enqueue(r);
    }
}

public abstract class Array : Object
{
    public abstract ClrArray BaseValue { get; }
}