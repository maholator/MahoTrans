namespace java.io;

public class StdOut : OutputStream
{
    public new void write(int b) => Toolkit.System.PrintOut((byte)((uint)b & 0xFF));
}