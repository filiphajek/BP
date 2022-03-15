using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;

namespace TaskLauncher.Api.Controllers;

public class Ban2Controller : BaseController
{
    private readonly AppDbContext context;

    public Ban2Controller(ILogger<Ban2Controller> logger, AppDbContext context) 
        : base(logger)
    {
        this.context = context;
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