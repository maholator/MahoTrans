// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Handles;

/// <summary>
/// Handle for a displayable.
/// </summary>
[PublicAPI]
public readonly struct DisplayableHandle : IEquatable<DisplayableHandle>
{
    /// <summary>
    /// Displayable ID in toolkit memory.
    /// </summary>
    public readonly int Id;

    public DisplayableHandle(int id)
    {
        Id = id;
    }

    public bool Equals(DisplayableHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is DisplayableHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(DisplayableHandle left, DisplayableHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DisplayableHandle left, DisplayableHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator int(DisplayableHandle dh) => dh.Id;
}