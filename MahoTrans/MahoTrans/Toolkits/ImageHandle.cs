// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Toolkits;

public readonly struct ImageHandle : IEquatable<ImageHandle>
{
    public readonly int Id;

    public ImageHandle(int id)
    {
        Id = id;
    }

    public bool Equals(ImageHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ImageHandle other && Equals(other);
    }

    public override int GetHashCode() => Id;

    public static bool operator ==(ImageHandle left, ImageHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ImageHandle left, ImageHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator int(ImageHandle ih) => ih.Id;
}