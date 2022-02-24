using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Models;
using TaskLauncher.WebApp.Server.Auth0;

namespace TaskLauncher.WebApp.Server.Controllers;

/// <summary>
/// Kontroler zajistujici presmerovani na prihlaseni pres oidc
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory clientFactory;
    private readonly ILogger<AuthController> logger;
    private readonly Auth0ApiConfiguration config;

    public AuthController(IHttpClientFactory clientFactory, ILogger<AuthController> logger, IOptions<Auth0ApiConfiguration> config)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.config = config.Value;
    }

    /// <summary>
    /// Login challenge endpoint
    /// </summary>
    [HttpGet("login")]
    public async Task Login()
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
        await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    /// <summary>
    /// Logout endpoint
    /// </summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpGet("logout")]
    public async Task Logout()
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
        await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

    /// <summary>
    /// muze slouzit pro pristup na API nebo pouze jako autorizace sem na server z aplikace
    /// https://auth0.com/docs/get-started/authentication-and-authorization-flow/call-your-api-using-resource-owner-password-flow
    /// </summary>
    [HttpPost("/m2m")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginM2MAsync(CookieLessLoginRequest request)
    {
        var response = await clientFactory.CreateClient().PostAsJsonAsync($"https://{config.Domain}/oauth/token", new
        {
            password = request.Password, // Password123*
            username = request.Name, // testuser@example.com
            grant_type = "password",
            audience = config.Audience,
            client_id = config.ClientId,
            client_secret = config.ClientSecret,
            scope = "openid offline_access"
        });

        if (!response.IsSuccessStatusCode)
            return Unauthorized();
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return Ok(result);
    }

    /// <summary>
    /// Refresh token endpoint
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("/refresh")]
    public async Task<IActionResult> RefreshTokenAsync(string token)
    {
        var client = clientFactory.CreateClient();
        var response = await client.PostAsJsonAsync($"https://{config.Domain}/oauth/token", new
        {
            grant_type = "refresh_token",
            client_id = config.ClientId,
            refresh_token = token
        });

        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("/test")]
    public IActionResult AuthorizedEndpoints()
    {
        return Ok(new { value = "hello "});
    }
}

public record CookieLessLoginRequest(string Name, string Password);
public record AuthResponse(string access_token, string refresh_token, string id_token, string token_type, int expires_in);
public record RefreshTokenResponse(string access_token, string scope, string token_type, int expires_in);
