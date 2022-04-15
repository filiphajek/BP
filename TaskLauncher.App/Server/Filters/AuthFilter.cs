using Auth0.AspNetCore.Authentication;
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
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Filters;

/// <summary>
/// Filter, ktery odhali a odhlasi zabanovaneho uzivatele, aktualizuje informace o uzivateli (zmena vip, tokenu apod.)
/// </summary>
public class AuthFilter : IAsyncAuthorizationFilter
{
    private readonly Cache<UserClaimsModel> cache;
    private readonly IAuth0UserProvider userProvider;
    private readonly ILogger<AuthFilter> logger;
    private readonly IConfiguration configuration;
    private readonly AppDbContext dbContext;

    public AuthFilter(Cache<UserClaimsModel> cache, IAuth0UserProvider userProvider, ILogger<AuthFilter> logger, IConfiguration configuration, AppDbContext dbContext)
    {
        this.cache = cache;
        this.userProvider = userProvider;
        this.logger = logger;
        this.configuration = configuration;
        this.dbContext = dbContext;
    }

    private async Task UpdateBalanceToken(AuthenticateResult auth)
    {
        var balance = await dbContext.TokenBalances.SingleAsync();
        auth.AddOrUpdateClaim(new Claim("token_balance", balance.CurrentAmount.ToString()));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        //pokud je to pristup na signalr nebo gty .. ignoruj
        if (context.HttpContext.User.TryGetClaimValue("azp", out var azp) && azp == configuration["ProtectedApiAzp"])
            return;
        if (context.HttpContext.User.TryGetClaimValue("gty", out var gty) && gty == "password")
            return;

        if (context.HttpContext.User.TryGetAuth0Id(out var userId))
        {
            if (context.HttpContext.User.IsInRole(Constants.Roles.Admin))
                return;

            if (context.HttpContext.User.TryGetClaimAsBool(Constants.ClaimTypes.Registered, out var value) && !value)
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
                        //auth0 exception
                        logger.LogInformation("Exception: {0}", ex);
                        context.Result = new StatusCodeResult(500);
                        return;
                    }
                    //uzivatel je blocknut, je treba ho odhlasit
                    logger.LogInformation("User is blocked {0} {1}", userId, ex);
                    var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
                    await context.HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Result = new StatusCodeResult(403);
                    return;
                }
                catch (Exception ex)
                {
                    //interni chyba
                    logger.LogInformation("Exception: {0}", ex);
                    context.Result = new StatusCodeResult(500);
                    return;
                }
            }

            //uzivatel je blocknut, je treba ho odhlasit
            if (cachedUserClaims.Blocked)
            {
                var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
                await context.HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = new StatusCodeResult(403);
                return;
            }

            //akualizace claim principal
            var auth = await context.HttpContext.AuthenticateCookieAsync();
            if (auth is null || auth.Principal is null)
                return;

            auth.AddOrUpdateClaim(new Claim(Constants.ClaimTypes.Vip, cachedUserClaims.Vip.ToString().ToLower()));
            auth.AddOrUpdateClaim(new Claim(Constants.ClaimTypes.EmailVerified, cachedUserClaims.EmailVerified.ToString().ToLower()));

            await UpdateBalanceToken(auth);

            await context.HttpContext.SignInAsync(auth);
        }
    }
}
