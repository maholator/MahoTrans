using java.io;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class System : Object
{
    [JavaType(typeof(PrintStream))] public static Reference @out;
    [JavaType(typeof(PrintStream))] public static Reference err;

    [ClassInit]
    public static void Clinit()
    {
        @out = Heap.AllocateObject<SystemPrintStream>().This;
        err = Heap.AllocateObject<SystemPrintStream>().This;
    }

    public static int identityHashCode(Reference x)
    {
        return Toolkit.System.GetHashCode(Heap.ResolveObject(x));
    }

    [return: String]
    public static Reference getProperty([String] Reference key)
    {
        return new Reference(0);
    }

    public static long currentTimeMillis() => Toolkit.System.CurrentMillis;

    public static void gc()
    {
        
    }
}