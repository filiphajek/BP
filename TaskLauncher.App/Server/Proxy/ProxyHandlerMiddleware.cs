using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TaskLauncher.App.Server.Proxy;

/// <summary>
/// Bazova trida dedici ProxyMiddleware pro tridy implementujici proxy chovani pres YARP
/// Tridy dedici tuto tridou implementuji middleware ktery se vykona pri prichodu na nakonfigurovany endpoint pres YARP
/// </summary>
public abstract class ProxyHandlerMiddleware : ProxyMiddleware, IMiddleware
{
    protected readonly ReverseProxyHandlers options;

    public ProxyHandlerMiddleware(IOptions<ReverseProxyHandlers> options) : base()
    {
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        //ukonceni requestu pri chybe:
        var proxyContext = context.GetReverseProxyFeature();
        if (options.Handlers.TryGetValue(proxyContext.Route.Config.RouteId, out var handler))
        {
            if (handler == Name)
            {
                await HandleAsync(context);
            }
        }
        await next(context);
    }

    public abstract Task HandleAsync(HttpContext context);
}
