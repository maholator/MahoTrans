// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime.Types;

public interface IJavaEntity
{
    /// <summary>
    ///     Name of the entity, used inside JVM.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Name of the entity, used for display purposes. User can change it to make reverse-engineering easier. If null,
    ///     <see cref="Name" /> should be used, i.e. this is an overrider.
    /// </summary>
    string? DisplayableName { get; set; }
}
