using Hangfire;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Routines;
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Kontroler, ktery slouzi pouze pro administatora
/// Ziskava a nastavuje systemove hodnoty
/// </summary>
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

    /// <summary>
    /// Ziska vsechny konfiguracni hodnoty nebo pouze jednu hodnotu, pokud je specifikovan parametr 'key'
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfiguratioAsync(string? key = null)
    {
        if (key is null)
        {
            var list = await dbContext.Configs.ToListAsync();
            return Ok(list.Select(mapper.Map<ConfigResponse>));
        }

        if(!Constants.Configuration.IsConfigurationValue(key))
            return new BadRequestObjectResult(new { error = "This is not an configuration value" });

        var config = await dbContext.Configs.SingleAsync(i => i.Key == key);
        return Ok(mapper.Map<ConfigResponse>(config));
    }

    /// <summary>
    /// Aktualizuje systemovou hodnotu
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateConfigurationValueAsync(UpdateConfigValueRequest request)
    {
        var config = await dbContext.Configs.SingleOrDefaultAsync(i => i.Key == request.Key);
        if (config is null)
            return new BadRequestObjectResult(new { error = $"Configuration value '{request.Key}' does not exist" });

        switch (config.Type)
        {
            case Common.Enums.ConstantTypes.Number:
                if(!int.TryParse(request.Value, out _))
                    return new BadRequestObjectResult(new { error = "Value is not a number type" });
                break;
            case Common.Enums.ConstantTypes.String:
                break;
            default:
                throw new NotSupportedException(nameof(config.Type));
        }
        config.Value = request.Value;
        dbContext.Update(config);
        await dbContext.SaveChangesAsync();

        if (request.Key == Constants.Configuration.FileRemovalRoutine)
        {
            client.RemoveIfExists(nameof(FileDeletionRoutine));
            client.AddOrUpdate(nameof(FileDeletionRoutine), () => routine.Perform(), Cron.Minutely);
        }
        return Ok();
    }
}
