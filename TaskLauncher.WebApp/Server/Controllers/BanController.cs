using Auth0.ManagementApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaskLauncher.Api.Controllers;
using TaskLauncher.Api.DAL;
using TaskLauncher.Api.DAL.Entities;
using TaskLauncher.WebApp.Server.Auth0;

namespace TaskLauncher.WebApp.Server.Controllers;

[Authorize(Policy = "admin-policy")]
public class BanController : BaseController
{
    private readonly AppDbContext context;
    private readonly ManagementTokenService managementTokenService;

    public BanController(ILogger<BanController> logger, AppDbContext context, ManagementTokenService managementTokenService) : base(logger)
    {
        this.context = context;
        this.managementTokenService = managementTokenService;
    }

    [HttpPut]
    public async Task<IActionResult> BanUserAsync(BanUserRequest request)
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        ManagementApiClient auth0client = new(accessToken, "localhost:5001");

        var ban = new BanEntity { Description = request.Reason, UserId = request.UserId, Started = DateTime.Now };
        await context.Bans.AddAsync(ban);

        var tmp = await auth0client.Users.UpdateAsync(request.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject($"{{ 'banid': {ban.Id} }}"),
            Blocked = true
        });

        return Ok(tmp);
    }

    [HttpGet("cancel")]
    public async Task<IActionResult> UnBanUserAsync(string id)
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        ManagementApiClient auth0client = new(accessToken, "localhost:5001");
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

    [HttpGet("userdetail")]
    public async Task<IActionResult> GetAllIps()
    {
        var tmp = await context.Ips.ToListAsync();
        return Ok(tmp);
        //chcem videt bany, ip, balance, platby -> vse pres odata a na frontendu tam budou takovy karty
    }
}

public record BanUserRequest
{
    public string UserId { get; set; }
    public string Reason { get; set; }
}