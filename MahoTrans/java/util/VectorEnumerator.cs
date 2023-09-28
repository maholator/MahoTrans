using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class VectorEnumerator : Object, Enumeration
{
    public Reference Vector;
    public int index;

    [JavaIgnore]
    private Vector Resolve() => Heap.Resolve<Vector>(Vector);

    public bool hasMoreElements()
    {
        return index < Resolve().size();
    }

    public Reference nextElement()
    {
        var l = Resolve().List;
        if (index >= l.Count)
            Heap.Throw<NoSuchElementException>();
        var el = l[index];
        index++;
        return el;
    }
}