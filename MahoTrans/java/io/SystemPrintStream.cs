using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.io;

public class SystemPrintStream : PrintStream
{
    public void println([String] Reference str)
    {
        Toolkit.System.Print(Heap.ResolveString(str));
        println();
    }

    public void println()
    {
        Toolkit.System.Print(Environment.NewLine);
    }
}