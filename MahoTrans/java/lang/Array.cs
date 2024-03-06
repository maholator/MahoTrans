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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.As<T[]>(BaseArray);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseArray = value;
    }

    /// <summary>
    ///     Gets/sets values in <see cref="TypedArray" />, performs bounds checks. Throws
    ///     <see cref="ArrayIndexOutOfBoundsException" /> in case of failure.
    /// </summary>
    /// <param name="index"></param>
    public T this[int index]
    {
        get
        {
            var arr = TypedArray;
            if ((uint)index >= (uint)arr.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {arr.Length}");
            return arr[index];
        }
        set
        {
            var arr = TypedArray;
            if ((uint)index >= (uint)arr.Length)
                Jvm.Throw<ArrayIndexOutOfBoundsException>(
                    $"Attempt to access index {index} on array with length {arr.Length}");
            arr[index] = value;
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

    /// <summary>
    ///     Helper for cross-compiler. Slightly lightened version of <see cref="Array{T}.this" />.
    /// </summary>
    public static T GetValue<T>(T[] arr, int index)
    {
        if ((uint)index >= (uint)arr.Length)
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        return arr[index];
    }

    /// <summary>
    ///     Helper for cross-compiler. Slightly lightened version of <see cref="Array{T}.this" />.
    /// </summary>
    public static void SetValue<T>(T[] arr, int index, T value)
    {
        if ((uint)index >= (uint)arr.Length)
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        arr[index] = value;
    }

    /// <summary>
    ///     Helper for cross-compiler. Gets length of array, referenced by argument.
    /// </summary>
    public static int GetLength(Reference r) => Jvm.Resolve<Array>(r).Length;
}