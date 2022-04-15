using Microsoft.Extensions.Options;
using TaskLauncher.Authorization.Auth0;

namespace TaskLauncher.App.Server.Proxy;

/// <summary>
/// Middleware ktery pridava hlavicku Authorization, ve ktere je access token pro managemenet api
/// Dotaz se dale preposila na auth0, vse konfigurovane pres appsetting.json
/// </summary>
public class Auth0ManagementTokenMiddleware : ProxyHandlerMiddleware
{
    private readonly ManagementTokenService managementTokenService;

    public Auth0ManagementTokenMiddleware(IOptions<ReverseProxyHandlers> options, ManagementTokenService managementTokenService) : base(options)
    {
        this.managementTokenService = managementTokenService;
    }

    public override async Task HandleAsync(HttpContext context)
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        context.Request.Headers.Authorization = $"Bearer {accessToken}";
    }
}
