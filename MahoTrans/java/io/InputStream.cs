using Object = java.lang.Object;

namespace java.io;

public class InputStream : Object
{
    // methods below are stubs per CLDC docs: https://nikita36078.github.io/J2ME_Docs/docs/midp-2.0/java/io/InputStream.html#close() and so on
    public int available() => 0;

    public void close()
    {
    }

    public bool markSupported() => false;

    public void mark(int readlimit)
    {
    }

    public void reset()
    {
        Jvm.Throw<IOException>();
    }
}