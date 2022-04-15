using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Kontroler, ktery slouzi pouze pro administatora
/// Ziskava a nastavuje systemove hodnoty
/// </summary>
[Route("api/admin/[controller]")]
public class ConfigController : BaseController
{
    private readonly IMapper mapper;
    private readonly AppDbContext dbContext;

    public ConfigController(ILogger<ConfigController> logger, AppDbContext dbContext, IMapper mapper)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    /// <summary>
    /// Získá všechny konfigurační hodnoty nebo pouze jednu hodnotu, pokud je specifikován parametr 'key' (pro workera)
    /// </summary>
    /// <param name="key" example="tasktimeout">Identifikátor proměnné</param>
    [ProducesResponseType(typeof(ConfigResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.CanViewConfigPolicy)]
    [HttpGet("/api/worker/config")]
    public async Task<ActionResult<ConfigResponse>> GetConfiguratioWorkerAsync(string? key = null) => await GetConfiguratioAsync(key);

    /// <summary>
    /// Získá všechny konfigurační hodnoty nebo pouze jednu hodnotu, pokud je specifikován parametr 'key' (pro administrátora)
    /// </summary>
    /// <param name="key" example="autofileremove">Identifikátor proměnné</param>
    [ProducesResponseType(typeof(ConfigResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.AdminPolicy)]
    [HttpGet]
    public async Task<ActionResult<ConfigResponse>> GetConfiguratioAdminAsync(string? key = null) => await GetConfiguratioAsync(key);

    private async Task<ActionResult<ConfigResponse>> GetConfiguratioAsync(string? key = null)
    {
        if (key is null)
        {
            var list = await dbContext.Configs.ToListAsync();
            return Ok(list.Select(mapper.Map<ConfigResponse>));
        }

        if (!Constants.Configuration.IsConfigurationValue(key))
            return BadRequest(new ErrorMessageResponse("This is not an configuration value"));

        var config = await dbContext.Configs.SingleAsync(i => i.Key == key);
        return Ok(mapper.Map<ConfigResponse>(config));
    }

    /// <summary>
    /// Aktualizuje sysémovou konfigurační proměnnou
    /// </summary>
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ConfigResponse), 200)]
    [ProducesResponseType(typeof(ErrorMessageResponse), 400)]
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.AdminPolicy)]
    [HttpPut]
    public async Task<IActionResult> UpdateConfigurationValueAsync(UpdateConfigValueRequest request)
    {
        var config = await dbContext.Configs.SingleOrDefaultAsync(i => i.Key == request.Key);
        if (config is null)
            return BadRequest(new ErrorMessageResponse($"Configuration value '{request.Key}' does not exist"));

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
        return Ok(mapper.Map<ConfigResponse>(config));
    }
}
