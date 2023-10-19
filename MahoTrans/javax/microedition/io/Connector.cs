using MahoTrans.Native;
using MahoTrans.Runtime;
using IOException = java.io.IOException;
using Object = java.lang.Object;

namespace javax.microedition.io;

public class Connector : Object
{
    [return: JavaType(typeof(Connection))]
    public static Reference open([String] Reference name)
    {
        Jvm.Throw<IOException>();
        return default;
    }

    [return: JavaType(typeof(Connection))]
    public static Reference open([String] Reference name, int mode)
    {
        Jvm.Throw<IOException>();
        return default;
    }
}