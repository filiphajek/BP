using Auth0.AspNetCore.Authentication;
using Auth0.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskLauncher.Common.Extensions;
using TaskLauncher.WebApp.Server.Services;

namespace TaskLauncher.WebApp.Server.Filters;

public class BanFilter : IAsyncAuthorizationFilter
{
    private readonly IAuth0UserProvider userProvider;
    private readonly ILogger<BanFilter> logger;

    public BanFilter(IAuth0UserProvider userProvider, ILogger<BanFilter> logger)
    {
        this.userProvider = userProvider;
        this.logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.TryGetAuth0Id(out var userId))
        {
            try
            {
                //TODO zde je rate limit -- potreba si to dat do cache a po 30 sekundach to aktualizovat
                var userInfo = await userProvider.GetActualUser();
            }
            catch (ErrorApiException ex)
            {
                if (ex.Message != "user is blocked")
                {
                    logger.LogInformation("Exception: {0}", ex);
                    context.Result = new StatusCodeResult(500);
                    return;
                }
                logger.LogInformation("User is blocked {0} {1}", userId, ex);
                var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
                await context.HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = new StatusCodeResult(403);
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation("Exception: {0}", ex);
                context.Result = new StatusCodeResult(500);
                return;
            }
            //lze aktualizovat claimy zde s kazdym prichozim requestem - mozna nepujde protoze to je pred requstem (alternativa IActionFilter, middleware)
            //bud se ptam do databaze nebo na auth0
        }
        }
}
