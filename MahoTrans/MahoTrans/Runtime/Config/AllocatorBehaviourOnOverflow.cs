// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Runtime.Config;

public enum AllocatorBehaviourOnOverflow
{
    [Description("Expand the heap")] Expand,

    [Description("Throw OutOfMemoryError")]
    ThrowOutOfMem,

    [Description("Crash")] Crash
}