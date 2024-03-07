// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.util;

public class Stack : Vector
{
    [InitMethod]
    public new void Init()
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
