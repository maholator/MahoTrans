namespace MahoTrans.Toolkit;

public readonly struct GraphicsDescriptor : IEquatable<GraphicsDescriptor>
{
    public readonly int Id;

    public GraphicsDescriptor(int id)
    {
        Id = id;
    }

    public bool Equals(GraphicsDescriptor other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is GraphicsDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(GraphicsDescriptor left, GraphicsDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GraphicsDescriptor left, GraphicsDescriptor right)
    {
        return !left.Equals(right);
    }
}