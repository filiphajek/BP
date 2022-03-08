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

        var user = await auth0client.Users.GetAsync(request.UserId);
        if (user.Blocked.HasValue && user.Blocked.Value)
            return BadRequest();

        var ban = new BanEntity { Description = request.Reason, UserId = request.UserId, Started = DateTime.Now };
        await context.Bans.AddAsync(ban);

        var tmp = await auth0client.Users.UpdateAsync(request.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject($"{{ 'banid': {ban.Id} }}"),
            Blocked = true
        });

        await context.SaveChangesAsync();
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
        if (ban is null)
            return BadRequest();

        ban.Ended = DateTime.Now;
        context.Update(ban);
        await context.SaveChangesAsync();

        var tmp = (await auth0client.Users.UpdateAsync(id, new()
        {
            Blocked = false,
            AppMetadata = JsonConvert.DeserializeObject("{ 'banid': null }"),
        }));

        return Ok(tmp);
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