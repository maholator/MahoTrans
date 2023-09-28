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
        Heap.Throw<IOException>();
        return default;
    }
}