using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.m3g;

public class Graphics3D : Object
{
    [JavaIgnore] private static Graphics3D? inst;

    [return: JavaType(typeof(Graphics3D))]
    public static Reference getInstance()
    {
        inst ??= Heap.AllocateObject<Graphics3D>();
        return inst.This;
    }

    public void bindTarget(Reference obj)
    {
    }

    public void releaseTarget()
    {
    }
}