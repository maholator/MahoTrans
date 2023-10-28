using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.m3g;

public class Graphics3D : Object
{
    public static Reference Inst;

    [return: JavaType(typeof(Graphics3D))]
    public static Reference getInstance()
    {
        if (Inst.IsNull)
            Inst = Jvm.AllocateObject<Graphics3D>().This;
        return Inst;
    }

    public void bindTarget(Reference obj)
    {
    }

    public void releaseTarget()
    {
    }
}