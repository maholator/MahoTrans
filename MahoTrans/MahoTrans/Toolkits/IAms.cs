namespace MahoTrans.Toolkits;

public interface IAms : IToolkit
{
    void PauseMidlet();

    void ResumeMidlet();

    void DestroyMidlet();

    void PlatformRequest(string url);
}