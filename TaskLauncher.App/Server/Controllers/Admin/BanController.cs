using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;

namespace TaskLauncher.App.Server.Controllers.Admin;

public class BanController : AdminODataController<BanResponse>
{
    private readonly IMapper mapper;
    private readonly ManagementApiClientFactory clientFactory;
    private readonly Cache<UserClaimsModel> cache;

    public BanController(AppDbContext context, IMapper mapper, ManagementApiClientFactory clientFactory, Cache<UserClaimsModel> cache) : base(context)
    {
        this.mapper = mapper;
        this.clientFactory = clientFactory;
        this.cache = cache;
    }

    public override ActionResult<IQueryable<BanResponse>> Get()
    {
        return Ok(context.Bans.IgnoreQueryFilters().ProjectToType<BanResponse>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BanResponse>> GetDetail(Guid id)
    {
        var ban = await context.Bans.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();
        return Ok(mapper.Map<BanResponse>(ban));
    }

    [HttpPost]
    public async Task<IActionResult> BanUserAsync([FromBody] BanUserRequest request)
    {
        var auth0client = await clientFactory.GetClient();

        var user = await auth0client.Users.GetAsync(request.UserId);
        if (user.Blocked.HasValue && user.Blocked.Value)
            return BadRequest();

        var ban = new BanEntity { Description = request.Reason, Email = user.Email, UserId = request.UserId, Started = DateTime.Now };
        await context.Bans.AddAsync(ban);

        var tmp = (await auth0client.Users.UpdateAsync(request.UserId, new()
        {
            Blocked = true
        })).GetModel();

        await context.SaveChangesAsync();
        await UpdateCache(request.UserId, true);
        return Ok(tmp);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> UnBanUserAsync(string id)
    {
        var auth0client = await clientFactory.GetClient();

        var user = await auth0client.Users.GetAsync(id);
        if (user.Blocked.HasValue && !user.Blocked.Value)
            return BadRequest();

        var ban = await context.Bans.IgnoreQueryFilters().OrderByDescending(i => i.Started).FirstOrDefaultAsync(i => i.UserId == id);
        if (ban is null)
            return BadRequest();

        ban.Ended = DateTime.Now;
        context.Update(ban);
        await context.SaveChangesAsync();

        var tmp = (await auth0client.Users.UpdateAsync(id, new()
        {
            Blocked = false,
        })).GetModel();

        await UpdateCache(id, false);
        return Ok(tmp);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBanAsync(Guid id)
    {
        var ban = await context.Bans.SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();

        if (ban.Ended is not null)
            return new BadRequestObjectResult(new { error = "Cant delete ban. Ban is not cancel" });

        context.Remove(ban);
        await context.SaveChangesAsync();
        return Ok();
    }

    private async Task UpdateCache(string userId, bool blocked)
    {
        var cached = await cache.GetAsync(userId);
        if (cached is not null)
        {
            cached.Blocked = blocked;
            await cache.UpdateAsync(userId, cached);
        }
    }
}