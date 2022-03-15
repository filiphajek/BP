using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.Api.Controllers.User;

[Authorize(Policy = TaskLauncherPolicies.UserPolicy)]
public class UserBanController : BaseController
{
    private readonly AppDbContext context;

    public UserBanController(ILogger<UserBanController> logger, AppDbContext context) : base(logger)
    {
        this.context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBanAsync()
    {
        if (!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        return Ok(await context.Bans.OrderByDescending(i => i.Started).FirstOrDefaultAsync());
    }
}
