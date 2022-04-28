using MapsterMapper;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Common;
using TaskLauncher.App.DAL.Helpers;

namespace TaskLauncher.App.Server.Controllers.Admin;

/// <summary>
/// Stat kontroler pristupny pouze pro admina, vraci vsechny statistiky z cele aplikace
/// </summary>
[Authorize(Policy = Constants.Policies.AdminPolicy)]
[Route("api/admin/[controller]")]
public class StatsController : BaseController
{
    private readonly IMapper mapper;
    private readonly AppDbContext dbContext;

    public StatsController(ILogger<StatsController> logger, AppDbContext dbContext, IMapper mapper) : base(logger)
    {
        this.dbContext = dbContext;
        this.mapper = mapper;
    }

    /// <summary>
    /// Vrací obecné seskupené statistiky od všech uživatelů nebo obecné statistiky zadaného uživatele pres parametr userId
    /// </summary>
    /// <param name="userId" example="auth0|622033411a44b70076f2790">Id uživatele</param>
    [ProducesResponseType(typeof(List<UserStatResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<ActionResult<List<UserStatResponse>>> GetUserStats(string? userId = null)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var stats = await dbContext.Stats.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync();
            if (stats is null)
                return NotFound();
            return Ok(stats.Select(mapper.Map<UserStatResponse>));
        }

        //seskupene statistiky
        var tmp = await dbContext.Stats.IgnoreQueryFilters().GroupBy(i => i.IsVip).Select(i => new UserStatResponse
        {
            FailedTasks = i.Sum(i => i.FailedTasks),
            AllTaskCount = i.Sum(i => i.AllTaskCount),
            SuccessTasks = i.Sum(i => i.SuccessTasks),
            CrashedTasks = i.Sum(i => i.CrashedTasks),
            TimeoutedTasks = i.Sum(i => i.TimeoutedTasks),
            FinishedTaskCount = i.Sum(i => i.FinishedTaskCount),
            IsVip = i.Key

        }).ToListAsync();
        return Ok(tmp);
    }

    /// <summary>
    /// Vrací se kolekce statistik posledních 30 úloh za poslední den od zadaného uživatele.
    /// Pokud se přes parametr userId nespecifikuje uživatel, vrací se kolekce statistik posledních 50 úloh celého systému za poslední den.
    /// Statistiky udávají, kolik času daná úloha strávila ve frontě a ve worker aplikaci
    /// </summary>
    /// <param name="userId" example="auth0|622033411a44b70076f2790">Id uživatele</param>
    [Produces("application/json")]
    [HttpGet("times")]
    public async Task<ActionResult<List<TaskStatResponse>>> GetUserTasksTimes(string? userId = null)
    {
        var minDate = DateTime.Now.AddDays(-1);
        List<TaskStatResponse> list = new();
        var baseQuery = dbContext.Tasks
            .IgnoreQueryFilters()
            .Include(i => i.Events);

        if (!string.IsNullOrEmpty(userId))
        {
            foreach (var task in await baseQuery
                .Where(i => i.UserId == userId)
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

        foreach (var task in await baseQuery
            .Where(i => i.CreationDate >= minDate)
            .OrderByDescending(i => i.CreationDate)
            .Take(50)
            .ToListAsync())
        {
            var (TimeInQueue, CpuTime) = TaskTimesHelper.GetTimeStats(task);
            list.Add(new(task.IsPriority, task.Name, TimeInQueue, CpuTime));
        }
        return Ok(list);
    }

    /// <summary>
    /// Vrací se kolekce, kde položka je počet založených úloh za den.
    /// Vrací se založené úlohy za posledních 30 dní od zadaného uživatele, pokud není specifikován, vrátí se úlohy od všech uživatelů za posledních 30 dní.
    /// Musí se specifikovat, zda se budou vracet vip nebo normalní úlohy
    /// </summary>
    /// <param name="vip" example="true">Vip</param>
    /// <param name="userId" example="auth0|622033411a44b70076f2790">Id uživatele</param>
    [Produces("application/json")]
    [Authorize(Policy = Constants.Policies.CanViewGraphsPolicy)]
    [HttpGet("taskcountperdays")]
    public async Task<ActionResult<List<DayTaskCountResponse>>> GetTaskCountPerDays(bool vip, string? userId = null)
    {
        var minDate = DateTime.Now.AddDays(-30);

        if (!string.IsNullOrEmpty(userId))
        {
            var tmp1 = await dbContext.Tasks.IgnoreQueryFilters()
                .Where(i => i.IsPriority == vip)
                .Where(i => i.UserId == userId)
                .Where(i => i.CreationDate >= minDate)
                .GroupBy(i => i.CreationDate.Date)
                .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
                .ToListAsync();
            return Ok(tmp1);
        }
        var tmp2 = await dbContext.Tasks.IgnoreQueryFilters()
            .Where(i => i.IsPriority == vip)
            .Where(i => i.CreationDate >= minDate)
            .GroupBy(i => i.CreationDate.Date)
            .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
            .ToListAsync();
        return Ok(tmp2);
    }
}
