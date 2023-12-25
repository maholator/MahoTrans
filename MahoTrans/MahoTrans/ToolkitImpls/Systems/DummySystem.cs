using java.lang;
using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Systems;

public class DummySystem : ISystem
{
    public void PrintException(Throwable t)
    {
    }

    public void PrintOut(byte b)
    {
    }

    public void PrintErr(byte b)
    {
    }

    public string? GetProperty(string name)
    {
        return "Unknown";
    }

    public string TimeZone => "UTC";
}