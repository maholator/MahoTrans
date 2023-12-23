namespace java.io;

public class StdErr : OutputStream
{
    public new void write(int b) => Toolkit.System.PrintErr((byte)((uint)b & 0xFF));
}