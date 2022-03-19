using Auth0.AspNetCore.Authentication;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskLauncher.App.DAL;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;
using TaskLauncher.Authorization.Services;
using TaskLauncher.App.Server.Extensions;

namespace TaskLauncher.App.Server.Filters;

public class BanFilter : IAsyncAuthorizationFilter
{
    private readonly Cache<UserClaimsModel> cache;
    private readonly IAuth0UserProvider userProvider;
    private readonly ILogger<BanFilter> logger;
    private readonly AppDbContext dbContext;

    public BanFilter(Cache<UserClaimsModel> cache, IAuth0UserProvider userProvider, ILogger<BanFilter> logger, AppDbContext dbContext)
    {
        this.cache = cache;
        this.userProvider = userProvider;
        this.logger = logger;
        this.dbContext = dbContext;
    }

    private async Task UpdateBalanceToken(AuthenticateResult auth)
    {
        //bylo by fajn to nacachovat
        var balance = await dbContext.TokenBalances.SingleAsync();
        auth.AddOrUpdateClaim(new Claim("token_balance", balance.CurrentAmount.ToString()));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.TryGetAuth0Id(out var userId))
        {
            if (context.HttpContext.User.TryGetClaimAsBool(TaskLauncherClaimTypes.Registered, out var value) && !value)
                return;

            UserClaimsModel? cachedUserClaims = await cache.GetAsync(userId);
            if (cachedUserClaims is null)
            {
                try
                {
                    var userInfo = await userProvider.GetActualUser(); // je zde rate limit -> cache
                    cachedUserClaims = userInfo.GetUserClaims();
                    cache.Add(userId, cachedUserClaims);
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
            }
            //akualizace tokenu je automaticky (refresh token), lze aktualizovat i v IActionFilter, middleware

            /*var vipClaim = context.HttpContext.User.TryGetClaimValue(TaskLauncherClaimTypes.Vip, out var isVip);
            var emailClaim = context.HttpContext.User.TryGetClaimValue(TaskLauncherClaimTypes.EmailVerified, out var emailVerified);

            if(!vipClaim || !emailClaim)
                return;*/

            if (cachedUserClaims.Blocked)
            {
                var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
                await context.HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = new StatusCodeResult(403);
                return;
            }

            var auth = await context.HttpContext.AuthenticateCookieAsync();
            if (auth is null || auth.Principal is null)
                return;

            auth.AddOrUpdateClaim(new Claim(TaskLauncherClaimTypes.Vip, cachedUserClaims.Vip.ToString().ToLower()));
            auth.AddOrUpdateClaim(new Claim(TaskLauncherClaimTypes.EmailVerified, cachedUserClaims.EmailVerified.ToString().ToLower()));

            await UpdateBalanceToken(auth);

            await context.HttpContext.SignInAsync(auth);
        }
    }
}
