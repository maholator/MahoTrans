namespace java.io;

public class StdErr : OutputStream
{
    //TODO use separate method
    public void write(int b) => Toolkit.System.PrintOut((byte)((uint)b & 0xFF));
}