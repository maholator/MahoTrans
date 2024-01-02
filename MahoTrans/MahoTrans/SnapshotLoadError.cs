// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans;

public class SnapshotLoadError : JavaRuntimeError
{
    public SnapshotLoadError()
    {
    }

    public SnapshotLoadError(string? message) : base(message)
    {
    }

    public SnapshotLoadError(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}