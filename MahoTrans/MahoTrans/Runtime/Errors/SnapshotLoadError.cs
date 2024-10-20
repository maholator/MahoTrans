// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime.Errors;

public class SnapshotLoadError : JavaRuntimeError
{
    public SnapshotLoadError()
    {
    }

    public SnapshotLoadError(string? message)
        : base(message)
    {
    }

    public SnapshotLoadError(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
