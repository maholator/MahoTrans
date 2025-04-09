// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Runtime.Config;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    /// <summary>
    ///     Toolkits, used in this JVM. Do not set this field on the fly because it may be cached somewhere. Alter
    ///     implemenations inside it.
    /// </summary>
    public ToolkitCollection Toolkit;

    public ExecutionManner ExecutionManner;
    public AllocatorBehaviourOnOverflow OnOverflow;
    public GraphicsFlow GraphicsFlow;
    public MissingThingsHandling MissingHandling;
}
