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
        @out = Jvm.AllocateObject<SystemPrintStream>().This;
        err = Jvm.AllocateObject<SystemPrintStream>().This;
    }

    public static int identityHashCode(Reference x)
    {
        return Toolkit.System.GetHashCode(Jvm.ResolveObject(x));
    }

    [return: String]
    public static Reference getProperty([String] Reference key)
    {
        switch (Jvm.ResolveString(key))
        {
            case "microedition.platform":
                return Jvm.AllocateString("Nokia MT-292");
        }

        return new Reference(0);
    }

    public static long currentTimeMillis() => Toolkit.Clock.GetCurrentMs(Jvm.CycleNumber);

    public static void gc()
    {
        Jvm.RunGarbageCollector();
    }

    public static void arraycopy(Reference src, int src_position, Reference dst, int dst_position, int length)
    {
        var arr1 = Jvm.Resolve<Array>(src).BaseValue;
        var arr2 = Jvm.Resolve<Array>(dst).BaseValue;
        global::System.Array.Copy(arr1, src_position, arr2, dst_position, length);
    }
}