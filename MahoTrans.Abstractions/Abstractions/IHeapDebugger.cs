// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace MahoTrans.Abstractions;

/// <summary>
///     Toolkit that used by JVM to log all heap operations.
/// </summary>
public interface IHeapDebugger : IToolkit
{
    /// <summary>
    ///     Called when new object is created.
    /// </summary>
    /// <param name="obj">Allocated object.</param>
    void ObjectCreated(Reference obj);

    /// <summary>
    ///     Called when object is deleted. This is the last chance to resolve it from heap. Do not try to store it somewhere,
    ///     since at this point JVM already thinks it's deleted.
    /// </summary>
    /// <param name="obj">Object that will be removed from heap right after call to this.</param>
    void ObjectDeleted(Reference obj);

    /// <summary>
    ///     Called when snapshot of the JVM is taken.
    /// </summary>
    void SnapshotTaken();

    /// <summary>
    ///     Called when JVM is restored from snapshot.
    /// </summary>
    void SnapshotRestored();
}