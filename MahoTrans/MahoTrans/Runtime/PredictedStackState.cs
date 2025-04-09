// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Compiler;

namespace MahoTrans.Runtime;

public struct PredictedStackState
{
    public PrimitiveType[] StackBeforeExecution;
    public StackValuePurpose[] ValuesPoppedOnExecution;
}
