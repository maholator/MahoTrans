namespace MahoTrans.Toolkit;

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
}