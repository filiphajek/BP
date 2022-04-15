using Auth0.AspNetCore.Authentication;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
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
using TaskLauncher.Common;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Kontroler zajistujici presmerovani na prihlaseni pres oidc
/// Poskytuje login endpoint vracejici access token pro klasicke desktop, mobilni aplikace
/// Jsou zde i zakladni endpointy potrebne k autentizaci/autorizaci jako je reset hesla, user info na zaklade http contextu apod.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext context;
    private readonly IHttpClientFactory clientFactory;
    private readonly IClientFactory<ManagementApiClient> apiClientFactory;
    private readonly IClientFactory<AuthenticationApiClient> authApiClientFactory;
    private readonly IMapper mapper;
    private readonly Auth0ApiConfiguration config;
    private readonly Auth0Roles roles;

    public AuthController(AppDbContext context,
        IOptions<Auth0Roles> options,
        IHttpClientFactory clientFactory,
        IOptions<Auth0ApiConfiguration> config,
        IMapper mapper,
        IClientFactory<AuthenticationApiClient> authApiClientFactory, 
        IClientFactory<ManagementApiClient> apiClientFactory)
    {
        roles = options.Value;
        this.context = context;
        this.clientFactory = clientFactory;
        this.mapper = mapper;
        this.config = config.Value;
        this.authApiClientFactory = authApiClientFactory;
        this.apiClientFactory = apiClientFactory;
    }

    /// <summary>
    /// Pøihlášení pøes službu auth0, uživatel je pøesmìrován na auth0, pouze z prohlížeèe (cookie autentizace)
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
    /// Odhlášení uzivatele (cookie autentizace)
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
    /// Reset hesla (cookie autentizace)
    /// </summary>
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ChangePassword()
    {
        if(!User.TryGetAuth0Id(out _) || !User.TryGetClaimValue(ClaimTypes.Email, out var email))
            return Unauthorized();

        var authClient = await authApiClientFactory.GetClient();
        var responsee = await authClient.ChangePasswordAsync(new()
        {
            ClientId = config.ClientId,
            Connection = Constants.Auth0.DefaultConnection,
            Email = email,
        });
        return Ok(new ResetPasswordResponse(responsee));
    }

    /// <summary>
    /// Získání uživatelských dat pøihlášeného uživatele na základì http kontextu
    /// </summary>
    [Produces("application/json")]
    [HttpGet("user")]
    [AllowAnonymous]
    public ActionResult<UserInfo> GetUserData()
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
    /// Slouží pro pøístup na API, pro desktopové nebo mobilní aplikace
    /// </summary>
    [Consumes("application/json")]
    [Produces("application/json")]
    [HttpPost("/passwordflow/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginM2MAsync(CookieLessLoginRequest request)
    {
        var response = await clientFactory.CreateClient().PostAsJsonAsync($"https://{config.Domain}/oauth/token", new
        {
            password = request.Password,
            username = request.Name,
            grant_type = "password",
            audience = config.Audience,
            client_id = config.ClientId,
            client_secret = config.ClientSecret,
            scope = "openid profile email offline_access"
        });

        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return Ok(result);
    }

    /// <summary>
    /// Registrace uživatele, pro desktopové nebo mobilní aplikace
    /// </summary>
    [Consumes("application/json")]
    [Produces("application/json")]
    [AllowAnonymous]
    [HttpPost("/passwordflow/register")]
    public async Task<ActionResult<UserModel>> RegisterM2MAsync(CookieLessUserRegistrationModel request)
    {
        var auth0client = await apiClientFactory.GetClient();
        var user = await auth0client.Users.CreateAsync(new()
        {
            Connection = Constants.Auth0.DefaultConnection,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            EmailVerified = false,
            NickName = request.NickName,
            Password = request.Password,
        });

        //role
        await auth0client.Roles.AssignUsersAsync(roles.User, new() { Users = new[] { user.UserId } });
        //vip
        var resultUser = await auth0client.Users.UpdateAsync(user.UserId,
            new() { AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false, 'registered': true, 'isadmin': false }") });

        //init databaze
        var userId = user.UserId;
        var balanceConfig = await context.Configs.SingleAsync(i => i.Key == Constants.Configuration.StartTokenBalance);
        await context.TokenBalances.AddAsync(new() { CurrentAmount = int.Parse(balanceConfig.Value), LastAdded = DateTime.Now, UserId = userId });
        await context.Stats.AddAsync(new() { UserId = userId, IsVip = true });
        await context.Stats.AddAsync(new() { UserId = userId, IsVip = false });
        await context.SaveChangesAsync();

        return Ok(resultUser.GetModel());
    }

    /// <summary>
    /// Aktualizuje pøístupový token, pro desktopové nebo mobilní aplikace
    /// </summary>
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("/passwordflow/refresh/{token}")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshTokenAsync(string token)
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
    /// Dokonèení registrace na webové stránce, pouze pro neregistrované uživatele
    /// </summary>
    [Consumes("application/json")]
    [Authorize(Policy = Constants.Policies.UserNotRegistered)]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUpAsync(UserRegistrationModel request)
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        //prirad roli
        await auth0client.Users.AssignRolesAsync(userId, new AssignRolesRequest { Roles = new[] { roles.User } });

        //aktualizuj profil
        var updateRequest = mapper.Map<UserUpdateRequest>(request);
        updateRequest.Email = null;
        updateRequest.PhoneNumber = null;
        updateRequest.AppMetadata = JsonConvert.DeserializeObject("{ 'registered': true }");
        var result = await auth0client.Users.UpdateAsync(userId, updateRequest);
        if (result is null)
            return Unauthorized();

        //inicializuj databazi
        var balanceConfig = await context.Configs.SingleAsync(i => i.Key == Constants.Configuration.StartTokenBalance);
        await context.TokenBalances.AddAsync(new() { CurrentAmount = int.Parse(balanceConfig.Value), LastAdded = DateTime.Now, UserId = userId });
        await context.Stats.AddAsync(new() { UserId = userId, IsVip = true });
        await context.Stats.AddAsync(new() { UserId = userId, IsVip = false });
        await context.SaveChangesAsync();

        await AddClaimsToPrincipal(new Claim[] 
        {
            new Claim(ClaimTypes.Role, "user"),
            new Claim(Constants.ClaimTypes.Registered, "true"), 
            new Claim(Constants.ClaimTypes.TokenBalance, balanceConfig.Value)
        });

        return Ok();
    }

    /// <summary>
    /// Posílá verifikaèní email
    /// </summary>
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.EmailNotConfirmed)]
    [HttpPost("verify-email")]
    public async Task<ActionResult<Job>> SendEmailAsync()
    {
        var auth0client = await apiClientFactory.GetClient();
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var job = await auth0client.Jobs.SendVerificationEmailAsync(new()
        {
            ClientId = config.ClientId,
            UserId = userId,
        });
        return Ok(job);
    }
}
