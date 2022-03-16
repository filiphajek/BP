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

[Authorize(Policy = TaskLauncherPolicies.UserPolicy)]
public class UserController : BaseController
{
    private readonly AppDbContext context;
    private readonly ManagementApiClientFactory apiClientFactory;

    public UserController(ILogger<UserController> logger, AppDbContext context, ManagementApiClientFactory apiClientFactory) : base(logger)
    {
        this.context = context;
        this.apiClientFactory = apiClientFactory;
    }

    [HttpGet("ban")]
    public async Task<IActionResult> GetBanAsync()
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        return Ok(await context.Bans.OrderByDescending(i => i.Started).FirstOrDefaultAsync());
    }

    /// <summary>
    /// Ziska vsechny aktualni informace o prihlasenem uzivateli
    /// </summary>
    [Authorize(Policy = "user-policy")]
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

    [Authorize(Policy = "user-policy")]
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

    [Authorize(Policy = "user-policy")]
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
