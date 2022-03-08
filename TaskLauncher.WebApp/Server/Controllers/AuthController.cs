using Auth0.AspNetCore.Authentication;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using System.Security.Claims;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.DAL;
using TaskLauncher.Common.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Server.Controllers;

/// <summary>
/// Kontroler zajistujici presmerovani na prihlaseni pres oidc
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext context;
    private readonly IHttpClientFactory clientFactory;
    private readonly ILogger<AuthController> logger;
    private readonly ManagementApiClientFactory apiClientFactory;
    private readonly IMapper mapper;
    private readonly Auth0ApiConfiguration config;

    public AuthController(AppDbContext context, IHttpClientFactory clientFactory, ILogger<AuthController> logger, 
        IOptions<Auth0ApiConfiguration> config, ManagementApiClientFactory apiClientFactory, IMapper mapper)
    {
        this.context = context;
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.apiClientFactory = apiClientFactory;
        this.mapper = mapper;
        this.config = config.Value;
    }

    /// <summary>
    /// Login challenge endpoint
    /// </summary>
    [HttpGet("login")]
    public async Task Login()
    {
        var ip = await context.IpBans.SingleOrDefaultAsync(i => i.Ip == HttpContext.Connection.RemoteIpAddress!.ToString());
        if (ip is not null)
        {
            HttpContext.Response.StatusCode = 402;
            return;
        }

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
    public async Task<IActionResult> GetUserData()
    {
        if (User.Identity is null || !User.Identity.IsAuthenticated)
            return Ok(UserInfo.Anonymous);

        var client = new AuthenticationApiClient(new Uri($"https://{config.Domain}"));
        var response = await client.GetTokenAsync(new Auth0.AuthenticationApi.Models.RefreshTokenRequest
        {
            RefreshToken = await HttpContext.GetTokenAsync("refresh_token"),
            Audience = "https://wutshot-test-api.com",
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            Scope = "openid profile email",
            SigningAlgorithm = JwtSignatureAlgorithm.RS256
        });

        var auth = await HttpContext.AuthenticateAsync("Cookies");
        if (auth.Properties is null || auth.Principal is null)
            return Ok(UserInfo.Anonymous);

        auth.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);
        auth.Properties.UpdateTokenValue("access_token", response.AccessToken);
        auth.Properties.UpdateTokenValue("id_token", response.IdToken);
        
        await HttpContext.SignInAsync("Cookies", auth.Principal, auth.Properties);

        return Ok(CreateUserInfo(User));
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
    [HttpPost("/loginbypasswordflow")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginM2MAsync(CookieLessLoginRequest request)
    {
        var response = await clientFactory.CreateClient().PostAsJsonAsync($"https://{config.Domain}/oauth/token", new
        {
            password = request.Password,
            username = request.Name,
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

    [Authorize]
    [HttpPost("/signup")]
    public async Task<IActionResult> SignUpAsync(UserModel request)
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();
        
        var result = await auth0client.Users.UpdateAsync(userId, mapper.Map<UserUpdateRequest>(request));
        if(result is null)
            return BadRequest();
        return Ok();
    }
}
