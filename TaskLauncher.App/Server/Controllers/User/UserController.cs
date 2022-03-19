using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers.User;

public class UserController : BaseController
{
    private readonly AppDbContext context;
    private readonly ManagementApiClientFactory apiClientFactory;

    public UserController(ILogger<UserController> logger, AppDbContext context, ManagementApiClientFactory apiClientFactory) : base(logger)
    {
        this.context = context;
        this.apiClientFactory = apiClientFactory;
    }

    /// <summary>
    /// Smazani uctu a vsech spojenych dat
    /// </summary>
    [Authorize(Policy = TaskLauncherPolicies.CanCancelAccount)]
    [HttpDelete]
    public async Task<IActionResult> GetBanAsync()
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
        await context.SaveChangesAsync();

        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();
        await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

        var balance = await context.TokenBalances.SingleAsync();
        var auth0client = await apiClientFactory.GetClient();
        var user = (await auth0client.Users.GetAsync(userId)).GetModel();
        user.TokenBalance = balance.CurrentAmount.ToString();
        return Ok(user);
    }

    [Authorize(Policy = TaskLauncherPolicies.CanHaveProfilePolicy)]
    [HttpPut("picture")]
    public async Task<ActionResult<UserModel>> UpdateUserPicture(string url)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();

        var auth0client = await apiClientFactory.GetClient();
        var user = (await auth0client.Users.UpdateAsync(userId, new()
        {
            UserMetadata = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(new { picture = url }))
        })).GetModel();
        return Ok(user);
    }

    [Authorize(Policy = TaskLauncherPolicies.CanHaveProfilePolicy)]
    [HttpPut("nickname")]
    public async Task<ActionResult<UserModel>> UpdateUserNickname(string value)
    {
        if (!User.TryGetAuth0Id(out var userId))
            return BadRequest();
       
        var auth0client = await apiClientFactory.GetClient();
        var user = (await auth0client.Users.UpdateAsync(userId, new()
        {
            NickName = value
        })).GetModel();
        return Ok(user);
    }
}
