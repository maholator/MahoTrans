namespace MahoTrans.Toolkits;

public interface ILoadTimeLogger : IToolkit
{
    public void Log(LogLevel level, string className, string message);
}