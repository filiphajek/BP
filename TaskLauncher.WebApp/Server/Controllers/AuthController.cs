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
using Newtonsoft.Json;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
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

        var authenticationProperties = new AuthenticationProperties()
        {
            IsPersistent = false,
            RedirectUri = "/",
            ExpiresUtc = DateTime.UtcNow.AddHours(2),
        };
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
    /// Ziskani uzivatelskych dat prihlaseneho uzivatele
    /// </summary>
    [HttpGet("user")]
    [AllowAnonymous]
    public IActionResult GetUserData()
    {
        if (User.Identity is null || !User.Identity.IsAuthenticated)
            return Ok(UserInfo.Anonymous);
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

    /// <summary>
    /// Dokonceni registrace
    /// </summary>
    [Authorize(Policy = "not-registered")]
    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUpAsync(UserRegistrationModel request)
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();

        //assign role
        await auth0client.Users.AssignRolesAsync("auth0|" + userId, new AssignRolesRequest { Roles = new[] { "rol_6Vh7zpX3Z61sN307" } });

        //update profile
        var updateRequest = mapper.Map<UserUpdateRequest>(request);
        updateRequest.Email = null;
        updateRequest.PhoneNumber = null;
        updateRequest.AppMetadata = JsonConvert.DeserializeObject("{ 'registered': true }");
        var result = await auth0client.Users.UpdateAsync("auth0|" + userId, updateRequest);
        if (result is null)
            return BadRequest(result);

        //refresh tokens
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

        //refresh claimsprincipal
        var auth = await HttpContext.AuthenticateAsync("Cookies");
        auth.Properties!.UpdateTokenValue("refresh_token", response.RefreshToken);
        auth.Properties!.UpdateTokenValue("access_token", response.AccessToken);
        auth.Properties!.UpdateTokenValue("id_token", response.IdToken);

        var claimsIdentity = (auth.Principal!.Identity as ClaimsIdentity)!;
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(response.IdToken);

        var claimsToRemove = claimsIdentity.Claims.Where(i => i.Type != ClaimTypes.NameIdentifier).Where(i => i.Type != ClaimTypes.Name).ToList();
        foreach(var claim in claimsToRemove)
        {
            claimsIdentity.TryRemoveClaim(claim);
        }
        claimsIdentity.AddClaims(jsonToken.Claims);

        await HttpContext.SignInAsync("Cookies", auth.Principal, auth.Properties);

        //init databaze
        var balanceConfig = await context.Configs.SingleAsync(i => i.Key == "starttokenbalance");
        await context.TokenBalances.AddAsync(new() { CurrentAmount = int.Parse(balanceConfig.Value), LastAdded = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Policy = "email-not-confirmed")]
    [HttpPost("email")]
    public async Task<IActionResult> SendEmailAsync()
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();
        userId = "auth0|" + userId;
        var job = await auth0client.Jobs.SendVerificationEmailAsync(new()
        {
            ClientId = config.ClientId,
            UserId = userId,
        });
        return Ok(job);
    }
}
