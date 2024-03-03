// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Newtonsoft.Json;
using ClrArray = System.Array;

namespace java.lang;

[JavaIgnore]
public class Array<T> : Array where T : struct
{
    /// <summary>
    ///     Underlying CLR array. Do not do direct accesses to it if there is a chance that index is invalid, use
    ///     <see cref="this" /> instead for proper bounds checks.
    /// </summary>
    [JsonIgnore]
    public T[] TypedArray
    {
        get => Unsafe.As<T[]>(BaseArray);
        set => BaseArray = value;
    }

    /// <summary>
    ///     Gets/sets values in <see cref="Value" />, performs bounds checks. Throws
    ///     <see cref="ArrayIndexOutOfBoundsException" /> in case of failure.
    /// </summary>
    /// <param name="index"></param>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)TypedArray.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {TypedArray.Length}");
            return TypedArray[index];
        }
        set
        {
            if ((uint)index >= (uint)TypedArray.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {TypedArray.Length}");
            TypedArray[index] = value;
        }
    }

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        if (typeof(T) != typeof(Reference))
            return;
        foreach (var r in (Reference[])BaseArray)
            queue.Enqueue(r);
    }

    public static Array<T> Create(T[] underlying, JavaClass cls)
    {
        var arr = new Array<T>();
        arr.TypedArray = underlying;
        arr.Length = underlying.Length;
        arr.JavaClass = cls;
        return arr;
    }

    public static Array<T> CreateEmpty(int length, JavaClass cls)
    {
        return Create(new T[length], cls);
    }
}

public abstract class Array : Object
{
    /// <summary>
    ///     Underlying array. Never set it manually! Note that its type may differ from "java" type due to various quirks.
    /// </summary>
    [JavaIgnore] public ClrArray BaseArray = null!;

    [JavaIgnore] [JsonProperty] public int Length;
}