using java.lang;
using Object = java.lang.Object;

namespace MahoTrans.Toolkit;

public interface ISystem
{
    int GetHashCode(Object obj);
    void PrintException(Throwable t);
    void Print(string s);
}