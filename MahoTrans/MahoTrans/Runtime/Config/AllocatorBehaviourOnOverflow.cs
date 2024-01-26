// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Runtime.Config;

/// <summary>
///     What to do if there is not enough heap slots?
/// </summary>
public enum AllocatorBehaviourOnOverflow
{
    [Description("Expand the heap")] Expand,

    [Description("Throw OutOfMemoryError")]
    ThrowOutOfMem,

    [Description("Crash")] Crash
}