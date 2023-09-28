namespace MahoTrans.Runtime;

public class VirtualPointer
{
    public readonly int Pointer;
    public readonly int ArgsCount;

    public VirtualPointer(int pointer, int argsCount)
    {
        Pointer = pointer;
        ArgsCount = argsCount;
    }
}