// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Runtime : Object
{
    [ClassInit]
    public static void ClInit()
    {
        NativeStatics.RuntimeInstance = Reference.Null;
    }

    [return: JavaType(typeof(Runtime))]
    public static Reference getRuntime()
    {
        if (NativeStatics.RuntimeInstance.IsNull)
        {
            NativeStatics.RuntimeInstance = Jvm.AllocateObject<Runtime>().This;
        }

        return NativeStatics.RuntimeInstance;
    }

    public void gc() => System.gc();

    public long freeMemory() => Jvm.FreeMemory;

    public long totalMemory() => Jvm.TotalMemory;
}