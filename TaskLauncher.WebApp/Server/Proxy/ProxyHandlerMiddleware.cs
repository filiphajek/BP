using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TaskLauncher.WebApp.Server.Proxy;

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
        //context.Response.StatusCode = 402;
        //return;
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
