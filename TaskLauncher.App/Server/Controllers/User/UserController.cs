using Auth0.AspNetCore.Authentication;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers.User;

/// <summary>
/// Kontroler slouzici pro uzivatele, poskytuje metody pro upravu profilu, zruseni uctu apod.
/// </summary>
public class UserController : BaseController
{
    private readonly AppDbContext context;
    private readonly IClientFactory<ManagementApiClient> apiClientFactory;

    public UserController(ILogger<UserController> logger, AppDbContext context, IClientFactory<ManagementApiClient> apiClientFactory) : base(logger)
    {
        this.context = context;
        this.apiClientFactory = apiClientFactory;
    }

    /// <summary>
    /// Smazani uctu a vsech vytvorenych dat
    /// </summary>
    [Authorize(Policy = TaskLauncherPolicies.CanCancelAccount)]
    [HttpDelete]
    public async Task<IActionResult> DeleteAccountAsync()
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        var auth0client = await apiClientFactory.GetClient();
        await auth0client.Users.DeleteAsync(userId);

        context.Payments.RemoveRange(await context.Payments.ToListAsync());
        context.Tasks.RemoveRange(await context.Tasks.ToListAsync());
        context.TokenBalances.RemoveRange(await context.TokenBalances.ToListAsync());
        context.Bans.RemoveRange(await context.Bans.ToListAsync());
        context.Events.RemoveRange(await context.Events.ToListAsync());
        context.Stats.RemoveRange(await context.Stats.ToListAsync());

        await context.SaveChangesAsync();

        try
        {
            //kvuli pristupu pres access token
            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
            await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch { }

        return Ok();
    }

    /// <summary>
    /// Ziska vsechny aktualni informace o prihlasenem uzivateli
    /// </summary>
    [Authorize(Policy = TaskLauncherPolicies.CanHaveProfilePolicy)]
    [HttpGet]
    public async Task<ActionResult<UserModel>> GetUserData()
    {
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();

        var auth0client = await apiClientFactory.GetClient();
        var user = (await auth0client.Users.GetAsync(userId)).GetModel();

        if(User.IsInRole(TaskLauncherRoles.User))
        {
            var balance = await context.TokenBalances.SingleAsync();
            user.TokenBalance = balance.CurrentAmount.ToString();
        }
        return Ok(user);
    }

    /// <summary>
    /// Upraveni uzivatelskeho profilu
    /// </summary>
    [HttpPatch]
    [Authorize(Policy = TaskLauncherPolicies.CanHaveProfilePolicy)]
    public async Task<ActionResult<UserModel>> UpdateUserProfile([FromBody] JsonPatchDocument<UpdateProfileRequest> patchUser)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        UpdateProfileRequest request = new();
        patchUser.ApplyTo(request);

        var auth0client = await apiClientFactory.GetClient();
        UserUpdateRequest userUpdate = new();

        if (!string.IsNullOrEmpty(request.Nickname))
            userUpdate.NickName = request.Nickname;
        if (!string.IsNullOrEmpty(request.Picture))
            userUpdate.UserMetadata = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(new { picture = request.Picture }));

        var user = (await auth0client.Users.UpdateAsync(userId, userUpdate)).GetModel();
        return Ok(user);
    }

    /// <summary>
    /// Zobrazi kontakt na jednoho z administratoru, kazdemu uzivateli se muze zobrazit jiny
    /// </summary>
    [Authorize(Roles = TaskLauncherRoles.User)]
    [HttpGet("admin-contact")]
    public async Task<ActionResult<AssignedUser>> GetContactToAdmin()
    {
        Random random = new(DateTime.Now.Millisecond);
        var auth0client = await apiClientFactory.GetClient();
        var admins = (await auth0client.Roles.GetUsersAsync("rol_biIw4J5bu8EQIReX")).ToList();
        var next = random.Next(admins.Count);
        return Ok(admins.ElementAt(next));
    }
}
