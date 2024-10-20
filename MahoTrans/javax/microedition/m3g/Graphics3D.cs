// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.m3g;

public class Graphics3D : Object
{
    [return: JavaType(typeof(Graphics3D))]
    public static Reference getInstance()
    {
        if (NativeStatics.Graphics3DInstance.IsNull)
            NativeStatics.Graphics3DInstance = Jvm.Allocate<Graphics3D>().This;
        return NativeStatics.Graphics3DInstance;
    }

    public void bindTarget(Reference obj)
    {
    }

    public void releaseTarget()
    {
    }
}
