namespace MahoTrans.Expressions;

public readonly struct JavaLabel
{
    public readonly int Id;

    public JavaLabel(int id)
    {
        Id = id;
    }

    public static implicit operator JavaLabel(int id)
    {
        return new JavaLabel(id);
    }

    public static implicit operator int(JavaLabel label)
    {
        return label.Id;
    }
}