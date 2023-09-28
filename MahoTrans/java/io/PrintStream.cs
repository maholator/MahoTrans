using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.io;

public class PrintStream : OutputStream
{
    [JavaType(typeof(OutputStream))] private Reference Output;


    [InitMethod]
    public void Init([JavaType(typeof(OutputStream))] Reference r) => Output = r;
    
}