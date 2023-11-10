using MahoTrans.Toolkits;

namespace MahoTrans.ToolkitImpls.Ams;

/// <summary>
/// Ams implementation which redirects all events from midlet to event listeners in the object.
/// Frontend must subscribe to events and do appropriate things.
/// </summary>
public class AmsEventHub : IAms
{
    public event Action? OnDestroy;
    public event Action? OnPause;
    public event Action? OnResume;
    public Action<string>? OnRequest;

    public void PauseMidlet()
    {
        OnPause?.Invoke();
    }

    public void ResumeMidlet()
    {
        OnResume?.Invoke();
    }

    public void DestroyMidlet()
    {
        OnDestroy?.Invoke();
    }

    public void PlatformRequest(string url)
    {
        OnRequest?.Invoke(url);
    }
}