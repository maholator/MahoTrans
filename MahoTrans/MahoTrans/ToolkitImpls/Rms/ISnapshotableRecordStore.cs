// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Abstractions;

namespace MahoTrans.ToolkitImpls.Rms;

/// <summary>
/// <see cref="IRecordStore"/> that can copy itself into <see cref="VirtualRms"/>.
/// </summary>
public interface ISnapshotableRecordStore : IRecordStore
{
    /// <summary>
    /// Copies content of this record store to <see cref="VirtualRms"/>. This must be deep (detached) clone. No futher modifications of original store or snapshot must modify another one.
    /// </summary>
    /// <returns>Detached copy of content of this store as <see cref="VirtualRms"/>.</returns>
    VirtualRms TakeSnapshot();
}