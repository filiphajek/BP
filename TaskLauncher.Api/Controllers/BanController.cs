using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Common.Auth0;

namespace TaskLauncher.Api.Controllers;

public class BanController : BaseController
{
    private readonly AppDbContext context;
    private readonly ManagementApiClientFactory clientFactory;

    public BanController(ILogger<BanController> logger, AppDbContext context, ManagementApiClientFactory clientFactory) : base(logger)
    {
        this.context = context;
        this.clientFactory = clientFactory;
    }

    [Authorize(Policy = "admin-policy")]
    [HttpPut]
    public async Task<IActionResult> BanUserAsync(BanUserRequest request)
    {
        var auth0client = await clientFactory.GetClient();

        var ban = new BanEntity { Description = request.Reason, UserId = request.UserId, Started = DateTime.Now };
        await context.Bans.AddAsync(ban);

        var tmp = await auth0client.Users.UpdateAsync(request.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject($"{{ 'banid': {ban.Id} }}"),
            Blocked = true
        });

        return Ok(tmp);
    }

    [Authorize(Policy = "admin-policy")]
    [HttpGet("cancel")]
    public async Task<IActionResult> UnBanUserAsync(string id)
    {
        var auth0client = await clientFactory.GetClient();

        var user = await auth0client.Users.GetAsync(id);
        if (user.Blocked.HasValue && !user.Blocked.Value)
            return BadRequest();

        Guid banId = user.AppMetadata.banid.Value;
        var ban = await context.Bans.SingleOrDefaultAsync(i => i.Id == banId);

        var tmp = (await auth0client.Users.UpdateAsync(id, new()
        {
            Blocked = false,
            AppMetadata = JsonConvert.DeserializeObject("{ 'banid': null }"),
        }));

        return Ok(tmp);
    }

    [Authorize(Policy = "admin-policy")]
    [HttpGet]
    public IActionResult GetAllBans(string? userId = null)
    {
        if (userId is null)
            return Ok(context.Ips.IgnoreQueryFilters().ProjectToType<BanResponse>());

        var allIps = context.Ips.IgnoreQueryFilters().Where(i => i.UserId == userId).ProjectToType<BanResponse>();
        return Ok(allIps);
    }
}