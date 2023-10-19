namespace MahoTrans.Toolkits;

public readonly struct DisplayableHandle : IEquatable<DisplayableHandle>
{
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
}