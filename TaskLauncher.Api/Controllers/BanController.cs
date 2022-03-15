using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.Authorization.Auth0;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Api.Controllers;

public class BanController : BaseController
{
    private readonly Cache<UserClaimsModel> cache;
    private readonly AppDbContext context;
    private readonly ManagementApiClientFactory clientFactory;

    public BanController(Cache<UserClaimsModel> cache, ILogger<BanController> logger, AppDbContext context, ManagementApiClientFactory clientFactory) 
        : base(logger)
    {
        this.cache = cache;
        this.context = context;
        this.clientFactory = clientFactory;
    }

    [Authorize(Policy = "user-policy")]
    [HttpGet("user")]
    public async Task<IActionResult> GetBanAsync()
    {
        if(!User.TryGetAuth0Id(out var userId))
            return Unauthorized();

        return Ok(await context.Bans.OrderByDescending(i => i.Started).FirstOrDefaultAsync());
    }

    [Authorize(Policy = "admin-policy")]
    [HttpPost]
    public async Task<IActionResult> BanUserAsync(BanUserRequest request)
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

    private async Task UpdateCache(string userId, bool blocked)
    {
        var cached = await cache.GetAsync(userId);
        if (cached is not null)
        {
            cached.Blocked = blocked;
            await cache.UpdateAsync(userId, cached);
        }
    }

    [Authorize(Policy = "admin-policy")]
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

    [Authorize(Policy = "admin-policy")]
    [HttpDelete]
    public async Task<IActionResult> DeleteBanAsync(Guid id)
    {
        var ban = await context.Bans.SingleOrDefaultAsync(i => i.Id == id);
        if (ban is null)
            return NotFound();
        context.Remove(ban);
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("banusersip")]
    public async Task<IActionResult> BanUsersIpAddresses(string userId)
    {
        /*var ips = await Task.Delay(10); //get user

        foreach(var ip in ips)
        {
            ip.Address.Banned = true;
            context.Update(ip);
        }*/
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("banip")]
    public async Task<IActionResult> BanIpAddress(string address)
    {
        var ip = await context.IpBans.SingleOrDefaultAsync(i => i.Ip == address);
        if (ip is not null)
            return Ok();
        
        await context.IpBans.AddAsync(new IpBanEntity { Ip = address });
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("unbanip")]
    public async Task<IActionResult> UnBanIpAddress(string address)
    {
        var ip = await context.IpBans.SingleOrDefaultAsync(i => i.Ip == address);
        if(ip is null)
            return NotFound();

        context.Remove(ip);
        await context.SaveChangesAsync();
        return Ok();
    }
}