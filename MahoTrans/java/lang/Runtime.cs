using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Runtime : Object
{
    public static Reference Instance;

    [return: JavaType(typeof(Runtime))]
    public static Reference getRuntime()
    {
        if (Instance.IsNull)
        {
            Instance = Heap.AllocateObject<Runtime>().This;
        }

        return Instance;
    }

    public void gc() => System.gc();

    public long freeMemory() => 1024 * 1024 * 3;

    public long totalMemory() => 1024 * 1024 * 4;
}