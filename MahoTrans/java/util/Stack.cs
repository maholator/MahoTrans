using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.util;

public class Stack : Vector
{
    [InitMethod]
    public void Init()
    {
        base.Init();
    }

    public bool empty() => size() == 0;

    public Reference peek()
    {
        if (empty())
            Jvm.Throw<EmptyStackException>();
        return List[^1];
    }

    public Reference pop()
    {
        if (empty())
            Jvm.Throw<EmptyStackException>();
        var r = List[^1];
        List.RemoveAt(List.Count - 1);
        return r;
    }

    public Reference push(Reference o)
    {
        List.Add(o);
        return o;
    }
}