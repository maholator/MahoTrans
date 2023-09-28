using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class ArrayEnumerator : Object, Enumeration
{
    public Reference[] Value;
    public int index;

    public bool hasMoreElements()
    {
        return index < Value.Length;
    }

    public Reference nextElement()
    {
        if (index >= Value.Length)
            Heap.Throw<NoSuchElementException>();
        var el = Value[index];
        index++;
        return el;
    }
}