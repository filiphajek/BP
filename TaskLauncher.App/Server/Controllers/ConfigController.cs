using Hangfire;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Routines;

namespace TaskLauncher.App.Server.Controllers;

[Authorize(Policy = "admin-policy")]
public class ConfigController : BaseController
{
    private readonly IMapper mapper;
    private readonly IRecurringJobManager client;
    private readonly FileDeletionRoutine routine;
    private readonly AppDbContext dbContext;

    public ConfigController(ILogger<ConfigController> logger, IRecurringJobManager client, FileDeletionRoutine routine, AppDbContext dbContext, IMapper mapper)
        : base(logger)
    {
        this.client = client;
        this.routine = routine;
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetConfiguratioAsync(string? key = null)
    {
        if (key is null)
        {
            var list = await dbContext.Configs.ToListAsync();
            return Ok(list.Select(mapper.Map<ConfigResponse>));
        }

        var config = await dbContext.Configs.SingleOrDefaultAsync(i => i.Key == key);
        return Ok(mapper.Map<ConfigResponse>(config)); // je jedno ze to bude null
    }

    [HttpPost]
    public async Task<IActionResult> AddOrUpdateConfigurationValueAsync(AddOrUpdateConfigValueRequest request)
    {
        var config = await dbContext.Configs.SingleOrDefaultAsync(i => i.Key == request.Key);
        if (config is null)
        {
            await dbContext.AddAsync(mapper.Map<ConfigEntity>(request));
            await dbContext.SaveChangesAsync();
            return Ok();
        }
        mapper.Map(request, config);
        dbContext.Update(config);
        await dbContext.SaveChangesAsync();

        if (request.Key == "autofileremove")
        {
            client.RemoveIfExists(nameof(FileDeletionRoutine));
            client.AddOrUpdate(nameof(FileDeletionRoutine), () => routine.Perform(), Cron.Minutely);
        }
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveConfigValue(string key)
    {
        var config = await dbContext.Configs.SingleOrDefaultAsync(i => i.Key == key);
        if (config is null)
            return BadRequest(new { message = "This key does not exist" });
        if (!config.CanDelete)
            return BadRequest(new { message = "Cant delete this value" });

        dbContext.Remove(config);
        await dbContext.SaveChangesAsync();
        return Ok();
    }
}
