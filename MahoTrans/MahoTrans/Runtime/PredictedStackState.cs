// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Runtime;

public struct PredictedStackState
{
    public PrimitiveType[]? StackBeforeExecution;
    public int ValuesPoppedOnExecution;
}