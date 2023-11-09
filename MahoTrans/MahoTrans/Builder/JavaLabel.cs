namespace MahoTrans.Builder;

public readonly struct JavaLabel : IDisposable
{
    public readonly int Id;
    public readonly JavaMethodBuilder Builder;

    public JavaLabel(JavaMethodBuilder builder, int id)
    {
        Id = id;
        Builder = builder;
    }

    public static implicit operator int(JavaLabel label)
    {
        return label.Id;
    }

    public void Dispose()
    {
        Builder.BringLabel(this);
    }
}