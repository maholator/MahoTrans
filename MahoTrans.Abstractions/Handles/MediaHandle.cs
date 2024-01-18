// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace MahoTrans.Handles;

/// <summary>
///     Handle for media player.
/// </summary>
[PublicAPI]
public struct MediaHandle : IEquatable<MediaHandle>
{
    /// <summary>
    ///     ID of the player in toolkit memory.
    /// </summary>
    public readonly int Id;


    public MediaHandle(int id)
    {
        Id = id;
    }

    public bool Equals(MediaHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is MediaHandle other && Equals(other);
    }

    public override int GetHashCode() => Id;

    public static bool operator ==(MediaHandle left, MediaHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MediaHandle left, MediaHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator int(MediaHandle mh) => mh.Id;
}