using java.lang;

namespace MahoTrans.Toolkits;

public interface ISystem : IToolkit
{
    void PrintException(Throwable t);

    void PrintOut(byte b);

    string? GetProperty(string name);

    string TimeZone { get; }
}