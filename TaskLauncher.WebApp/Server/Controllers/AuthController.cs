using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Server.Controllers;

/// <summary>
/// Kontroler zajistujici presmerovani na prihlaseni pres oidc
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> logger;

    public AuthController(ILogger<AuthController> logger)
    {
        this.logger = logger;
    }

    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
    [HttpGet("login1")]
    public IActionResult Login1()
    {
        return Ok();
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties
        {
            RedirectUri = "/"
        },
        CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Ziskani access tokenu
    /// </summary>
    [Authorize]
    [HttpGet("accesstoken")]
    public async Task<IActionResult> GetAccessToken()
    {
        var token = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(token))
            return Unauthorized();
        return Ok(new { access = token });
    }

    /// <summary>
    /// Ziskani uzivatelskych dat prihlaseneho uzivatele
    /// </summary>
    [HttpGet("user")]
    [AllowAnonymous]
    public IActionResult GetUserData()
    {
        if (User.Identity is null)
            return Ok(UserInfo.Anonymous);
        return Ok(User.Identity.IsAuthenticated ? CreateUserInfo(User) : UserInfo.Anonymous);
    }

    private static UserInfo CreateUserInfo(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity is null)
            return UserInfo.Anonymous;

        if (!claimsPrincipal.Identity.IsAuthenticated)
            return UserInfo.Anonymous;

        var userInfo = new UserInfo
        {
            IsAuthenticated = true,
            NameClaimType = ClaimTypes.Name
        };

        userInfo.Claims.AddRange(claimsPrincipal.Claims.Select(i => new ClaimValue { Type = i.Type, Value = i.Value }));
        return userInfo;
    }
}
