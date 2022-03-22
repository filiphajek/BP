using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TaskLauncher.App.Server.Proxy;

public class Auth0AccessTokenMiddleware : ProxyHandlerMiddleware
{
    public Auth0AccessTokenMiddleware(IOptions<ReverseProxyHandlers> options) : base(options) { }

    public override async Task HandleAsync(HttpContext context)
    {
        var cache = "";

        if (string.IsNullOrEmpty(cache))
            cache = await context.GetTokenAsync("access_token");

        context.Request.Headers.Add("Authorization", $"Bearer {cache}");
    }
}