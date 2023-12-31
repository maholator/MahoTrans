using System.ComponentModel;

namespace MahoTrans.Runtime;

public enum AllocatorBehaviourOnOverflow
{
    [Description("Expand the heap")] Expand,

    [Description("Throw OutOfMemoryError")]
    ThrowOutOfMem,

    [Description("Crash")] Crash
}