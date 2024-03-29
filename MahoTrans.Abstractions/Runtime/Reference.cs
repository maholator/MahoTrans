// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using MahoTrans.Utils;
using Newtonsoft.Json;

namespace MahoTrans.Runtime;

/// <summary>
///     Pointer to java object in heap.
/// </summary>
[JsonConverter(typeof(ReferenceJsonConverter))]
[DebuggerDisplay("Reference to {Index}")]
public struct Reference : IEquatable<Reference>
{
    public readonly int Index;

    public Reference(int index)
    {
        Index = index;
    }

    public bool IsNull => Index == 0;
    public static Reference Null => new Reference(0);

    public override int GetHashCode()
    {
        return Index;
    }

    public override string ToString()
    {
        return Index.ToString();
    }

    public bool Equals(Reference other)
    {
        return Index == other.Index;
    }

    public override bool Equals(object? obj)
    {
        return obj is Reference other && Equals(other);
    }

    public static bool operator ==(Reference a, Reference b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Reference a, Reference b)
    {
        return !a.Equals(b);
    }

    public static implicit operator long(Reference r)
    {
        return r.Index;
    }
    
    public static implicit operator Reference(long pointer)
    {
        return new Reference((int)pointer);
    }

    public static implicit operator Reference(int pointer)
    {
        return new Reference(pointer);
    }
}
