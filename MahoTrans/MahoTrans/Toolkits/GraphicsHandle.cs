namespace MahoTrans.Toolkits;

public readonly struct GraphicsHandle : IEquatable<GraphicsHandle>
{
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