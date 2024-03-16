// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans;

/// <summary>
///     Base interface for all java objects.
/// </summary>
public interface IJavaObject
{
    Reference This { get; }

    bool OnObjectDelete();

    void AnnounceHiddenReferences(Queue<Reference> queue);
}
