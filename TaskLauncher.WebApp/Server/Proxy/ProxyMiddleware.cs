using System.Text.RegularExpressions;

namespace TaskLauncher.WebApp.Server.Proxy;

public abstract class ProxyMiddleware
{
    protected readonly string Name = string.Empty;

    public ProxyMiddleware()
    {
        Name = Regex.Replace(GetType().Name, "Middleware", "");
    }
}
