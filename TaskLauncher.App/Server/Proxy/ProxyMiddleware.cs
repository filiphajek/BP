using System.Text.RegularExpressions;

namespace TaskLauncher.App.Server.Proxy;

/// <summary>
/// Bazova trida pro tridy implementujici proxy chovani
/// </summary>
public abstract class ProxyMiddleware
{
    protected readonly string Name = string.Empty;

    public ProxyMiddleware()
    {
        Name = Regex.Replace(GetType().Name, "Middleware", "");
    }
}
