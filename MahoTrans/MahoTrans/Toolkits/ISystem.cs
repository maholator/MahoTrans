using java.lang;
using Object = java.lang.Object;

namespace MahoTrans.Toolkits;

public interface ISystem : IToolkit
{
    int GetHashCode(Object obj);

    void PrintException(Throwable t);

    void PrintOut(byte b);

    string? GetProperty(string name);
}