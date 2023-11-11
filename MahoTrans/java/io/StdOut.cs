namespace java.io;

public class StdOut : OutputStream
{
    public void write(int b) => Toolkit.System.PrintOut((byte)((uint)b & 0xFF));
}