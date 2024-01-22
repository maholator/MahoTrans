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
    /// <summary>
    /// Underlying CLR array. Do not do direct accesses to it if there is a chance that index is invalid, use <see cref="this"/> instead for proper bounds checks.
    /// </summary>
    [JavaIgnore] public T[] Value = null!;

    [JsonIgnore] public override ClrArray BaseValue => Value;

    /// <summary>
    /// Gets/sets values in <see cref="Value"/>, performs bounds checks. Throws <see cref="ArrayIndexOutOfBoundsException"/> in case of failure.
    /// </summary>
    /// <param name="index"></param>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Value.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {Value.Length}");
            return Value[index];
        }
        set
        {
            if ((uint)index >= (uint)Value.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {Value.Length}");
            Value[index] = value;
        }
    }

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