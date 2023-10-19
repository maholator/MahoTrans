using java.lang;
using Object = java.lang.Object;

namespace MahoTrans.Toolkits;

public interface ISystem
{
    int GetHashCode(Object obj);

    void PrintException(Throwable t);

    void PrintOut(string s);

    string? GetProperty(string name);

    long GetTotalMemory();

    long GetFreeMemory();
}