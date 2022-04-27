using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Helpers;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Common;

namespace TaskLauncher.App.Server.Controllers.User;

/// <summary>
/// Stat kontroler, ktery vraci statistiky pro prihlaseneho uzivatele
/// </summary>
[Authorize(Policy = Constants.Policies.UserPolicy)]
public class StatsController : BaseController
{
    private readonly IMapper mapper;
    private readonly AppDbContext dbContext;

    public StatsController(IMapper mapper, AppDbContext dbContext, ILogger<StatsController> logger) : base(logger)
    {
        this.mapper = mapper;
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Vrací obecné statistiky pro přihlášeného uživatele
    /// </summary>
    [ProducesResponseType(typeof(List<UserStatResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<ActionResult<List<UserStatResponse>>> GetUserStats()
    {
        var userStats = await dbContext.Stats.ToListAsync();
        if (userStats is null)
            return NotFound();
        return Ok(userStats.Select(mapper.Map<UserStatResponse>));
    }

    /// <summary>
    /// Vrací se kolekce statistik posledních 30 úloh za poslední den od přihlášeného uživatele
    /// Statistiky udaávají kolik času daná úloha strávila ve workeru a jak dlouho se počítala
    /// </summary>
    [Produces("application/json")]
    [HttpGet("times")]
    public async Task<ActionResult<List<TaskStatResponse>>> GetUserTasksTimes()
    {
        var minDate = DateTime.Now.AddDays(-1);
        List<TaskStatResponse> list = new();

        foreach (var task in await dbContext.Tasks
            .Include(i => i.Events)
            .Where(i => i.CreationDate >= minDate)
            .OrderByDescending(i => i.CreationDate)
            .Take(30)
            .ToListAsync())
        {
            var (TimeInQueue, CpuTime) = TaskTimesHelper.GetTimeStats(task);
            list.Add(new(task.IsPriority, task.Name, TimeInQueue, CpuTime));
        }
        return Ok(list);
    }

    /// <summary>
    /// Vrací kolekci kde položka je počet úloh spuštěných za den, jsou to úlohy za posledních 30 dní
    /// Musí se specifikovat, zda se budou vracet vip nebo normalní úlohy
    /// </summary>
    /// <param name="vip" example="true">Vip</param>
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.CanViewGraphsPolicy)]
    [HttpGet("taskcountperdays")]
    public async Task<ActionResult<List<DayTaskCountResponse>>> GetTaskCountPerDays(bool vip)
    {
        var minDate = DateTime.Now.AddDays(-30);
        var tmp = await dbContext.Tasks
            .Where(i => i.IsPriority == vip)
            .Where(i => i.CreationDate >= minDate)
            .GroupBy(i => i.CreationDate.Date)
            .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
            .ToListAsync();
        return Ok(tmp);
    }
}
