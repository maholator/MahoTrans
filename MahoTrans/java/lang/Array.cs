// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Newtonsoft.Json;
using ClrArray = System.Array;

namespace java.lang;

[JavaIgnore]
public class Array<T> : Array where T : struct
{
    [JavaIgnore] public T[] Value = null!;

    [JsonIgnore] public override ClrArray BaseValue => Value;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        if (typeof(T) != typeof(Reference))
            return;
        foreach (var r in (Reference[])(object)Value)
            queue.Enqueue(r);
    }
}

public abstract class Array : Object
{
    [JsonIgnore] public abstract ClrArray BaseValue { get; }
}