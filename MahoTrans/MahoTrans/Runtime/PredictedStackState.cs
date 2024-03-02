// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Compiler;

namespace MahoTrans.Runtime;

public struct PredictedStackState
{
    public PrimitiveType[] StackBeforeExecution;
    public StackValuePurpose[] ValuesPoppedOnExecution;
}