using Auth0.AspNetCore.Authentication;
using Auth0.ManagementApi.Models;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Claims;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Extensions;
using TaskLauncher.Authorization;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers;

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

    public AuthController(AppDbContext context,
        IHttpClientFactory clientFactory,
        ILogger<AuthController> logger,
        IOptions<Auth0ApiConfiguration> config,
        ManagementApiClientFactory apiClientFactory,
        IMapper mapper)
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

    private async Task AddClaimToPrincipal(Claim claim)
    {
        var auth = await HttpContext.AuthenticateCookieAsync();

        if (auth is null || auth.Principal is null)
            return;

        auth.AddOrUpdateClaim(claim);
        await HttpContext.SignInAsync(auth);
    }

    private async Task AddClaimsToPrincipal(IEnumerable<Claim> claims)
    {
        var auth = await HttpContext.AuthenticateCookieAsync();

        if (auth is null || auth.Principal is null)
            return;

        foreach (var claim in claims)
        {
            auth.AddOrUpdateClaim(claim);
        }
        await HttpContext.SignInAsync(auth);
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
        await auth0client.Users.AssignRolesAsync(userId, new AssignRolesRequest { Roles = new[] { "rol_6Vh7zpX3Z61sN307" } });

        //update profile
        var updateRequest = mapper.Map<UserUpdateRequest>(request);
        updateRequest.Email = null;
        updateRequest.PhoneNumber = null;
        updateRequest.AppMetadata = JsonConvert.DeserializeObject("{ 'registered': true }");
        var result = await auth0client.Users.UpdateAsync(userId, updateRequest);
        if (result is null)
            return BadRequest(result);

        //init databaze
        var balanceConfig = await context.Configs.SingleAsync(i => i.Key == "starttokenbalance");
        await context.TokenBalances.AddAsync(new() { CurrentAmount = int.Parse(balanceConfig.Value), LastAdded = DateTime.Now, UserId = userId });
        await context.SaveChangesAsync();

        await AddClaimsToPrincipal(new Claim[] 
        {
            new Claim(ClaimTypes.Role, "user"),
            new Claim(TaskLauncherClaimTypes.Registered, "true"), 
            new Claim("token_balance", "balanceConfig.Value") 
        });

        return Ok();
    }

    [Authorize(Policy = "email-not-confirmed")]
    [HttpPost("email")]
    public async Task<IActionResult> SendEmailAsync()
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();

        var job = await auth0client.Jobs.SendVerificationEmailAsync(new()
        {
            ClientId = config.ClientId,
            UserId = userId,
        });
        return Ok(job);
    }
}
