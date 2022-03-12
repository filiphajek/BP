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

namespace TaskLauncher.Api.Controllers;

[Authorize(Policy = "admin-policy")]
public class BanController : BaseController
{
    private readonly AppDbContext context;
    private readonly ManagementApiClientFactory clientFactory;

    public BanController(ILogger<BanController> logger, AppDbContext context, ManagementApiClientFactory clientFactory) : base(logger)
    {
        this.context = context;
        this.clientFactory = clientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(string? userId = null)
    {
        if (userId is null)
            return Ok(await context.Bans.ToListAsync());
        return Ok(await context.Bans.Where(i => i.UserId == userId).ToListAsync());
    }

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
        return Ok(tmp);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> UnBanUserAsync(string id)
    {
        id = "auth0|" + id;
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

        return Ok(tmp);
    }

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