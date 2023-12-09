namespace MahoTrans.Toolkits;

public interface ILoadTimeLogger : IToolkit
{
    public void Log(LoadIssueType type, string className, string message);
}