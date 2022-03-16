using Microsoft.AspNetCore.Authentication;
using TaskLauncher.App.Server.Extensions;

namespace TaskLauncher.App.Server.Extensions;

public static class HttpContextExtensions
{
    public static async Task<AuthenticateResult> AuthenticateCookieAsync(this HttpContext httpContext)
        => await httpContext.AuthenticateAsync("Cookies");

    public static async Task SignInAsync(this HttpContext httpContext, AuthenticateResult auth)
        => await httpContext.SignInAsync("Cookies", auth.Principal!, auth.Properties);
}
