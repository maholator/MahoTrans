// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime.Types;

namespace MahoTrans.Runtime;

public class ClassBoundBridge
{
    public readonly Action<Frame> Bridge;
    public readonly JavaClass Class;

    public ClassBoundBridge(Action<Frame> bridge, JavaClass @class)
    {
        Bridge = bridge;
        Class = @class;
    }
}
