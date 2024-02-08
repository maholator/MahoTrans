// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Config;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    public ToolkitCollection Toolkit;
    private readonly ExecutionManner _executionManner;
    public AllocatorBehaviourOnOverflow OnOverflow;
    public GraphicsFlow GraphicsFlow;
    public MissingThingsHandling MissingHandling;
    public bool UseBridgesForFields = true;
}