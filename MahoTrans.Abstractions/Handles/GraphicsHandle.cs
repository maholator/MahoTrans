// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Handles;

/// <summary>
/// Handle for a graphics object.
/// </summary>
[PublicAPI]
public readonly struct GraphicsHandle : IEquatable<GraphicsHandle>
{
    /// <summary>
    /// ID of the graphics object in toolkit memory.
    /// </summary>
    public readonly int Id;

    public GraphicsHandle(int id)
    {
        Id = id;
    }

    public bool Equals(GraphicsHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is GraphicsHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(GraphicsHandle left, GraphicsHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GraphicsHandle left, GraphicsHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator int(GraphicsHandle gh) => gh.Id;
}