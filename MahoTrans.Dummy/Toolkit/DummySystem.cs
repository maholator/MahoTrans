using java.lang;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace MahoTrans.Dummy.Toolkit;

public class DummySystem : ISystem
{
    public int GetHashCode(Object obj) => 0;

    public void PrintException(Throwable t)
    {
        //TODO
    }

    public void Print(string s)
    {
        Console.Write(s);
    }

    public long CurrentMillis => 0;
}