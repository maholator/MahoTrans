namespace MahoTrans.Toolkit;

public readonly struct DisplayableDescriptor : IEquatable<DisplayableDescriptor>
{
    public readonly int Id;

    public DisplayableDescriptor(int id)
    {
        Id = id;
    }

    public bool Equals(DisplayableDescriptor other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is DisplayableDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(DisplayableDescriptor left, DisplayableDescriptor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DisplayableDescriptor left, DisplayableDescriptor right)
    {
        return !left.Equals(right);
    }
}