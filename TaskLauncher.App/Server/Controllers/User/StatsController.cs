using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Helpers;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.Server.Controllers.User;

/// <summary>
/// Stat kontroler, ktery vraci statistiky pro prihlaseneho uzivatele
/// </summary>
[Authorize(Policy = TaskLauncherPolicies.UserPolicy)]
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
    /// Vraci obecne statistiky pro prihlaseneho uzivatele
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserStatResponse>>> GetUserStats()
    {
        var userStats = await dbContext.Stats.ToListAsync();
        if (userStats is null)
            return BadRequest();
        return Ok(userStats.Select(mapper.Map<UserStatResponse>));
    }

    /// <summary>
    /// Vraci se kolekce statistik poslednich 30 tasku za posledni den souvisejici od prihlaseho uzivatele
    /// Statistiky udavaji kolik casu dany task stravil ve fronte a ve workeru
    /// </summary>
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
    /// Vraci kolekci kde polozka je pocet tasku za den
    /// Vraci se zalozene tasky za poslednich 30 dni od prihlaseneho uzivatelu
    /// Musi se specifikovat zda se budou vracet vip nebo normalni tasky
    /// </summary>
    [Authorize(Policy = TaskLauncherPolicies.CanViewGraphsPolicy)]
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
