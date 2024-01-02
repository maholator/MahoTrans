// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Builder;

/// <summary>
///     Handle object to manipulate labels in bulding method.
///     Use <see cref="JavaMethodBuilder.BringLabel" /> to assing an instruction to this label.
/// </summary>
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