using System.Diagnostics;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace MahoTrans.Runtime;

/// <summary>
/// Pointer to java object in heap. Do <see cref="JvmState"/>.<see cref="JvmState.Resolve{T}"/> to get the object.
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

    /// <summary>
    /// Resolves this reference. For debug purposes.
    /// </summary>
    [JsonIgnore] public Object? Value => IsNull ? null : Object.Jvm.ResolveObject(Index);

    public Object AsObject()
    {
        return JvmState.Context.ResolveObject(this);
    }

    public T As<T>() where T : Object
    {
        return JvmState.Context.Resolve<T>(this);
    }
    public T? AsNullable<T>() where T : Object
    {
        return JvmState.Context.ResolveNullable<T>(this);
    }

    public override int GetHashCode()
    {
        return Index;
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