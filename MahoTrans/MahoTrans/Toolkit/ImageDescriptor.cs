namespace MahoTrans.Toolkit;

public readonly struct ImageDescriptor : IEquatable<ImageDescriptor>
{
    public readonly int Id;

    public ImageDescriptor(int id)
    {
        Id = id;
    }

    public bool Equals(ImageDescriptor other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ImageDescriptor other && Equals(other);
    }

    public override int GetHashCode() => Id;

    public static bool operator ==(ImageDescriptor left, ImageDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ImageDescriptor left, ImageDescriptor right)
    {
        return !left.Equals(right);
    }
}