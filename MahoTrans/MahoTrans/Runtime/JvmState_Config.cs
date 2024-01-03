// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Toolkits;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    public Toolkit Toolkit;
    private readonly ExecutionManner _executionManner;
    public AllocatorBehaviourOnOverflow OnOverflow;
    public bool UseBridgesForFields = true;
}