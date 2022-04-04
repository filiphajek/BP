using MapsterMapper;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskLauncher.Authorization;
using TaskLauncher.Api.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.Server.Helpers;

namespace TaskLauncher.App.Server.Controllers.Admin;

/// <summary>
/// Stat kontroler pristupny pouze pro admina, vraci vsechny statistiky z cele aplikace
/// </summary>
[Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
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
    /// Vraci obecne seskupene statistiky od vsech uzivatelu nebo obecne statistiky zadaneho uzivatele pres query parametr
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserStatResponse>>> GetUserStats(string? userId = null)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var stats = await dbContext.Stats.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync();
            if (stats is null)
                return BadRequest();
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
    /// Vraci se kolekce statistik poslednich 30 tasku za posledni den od zadaneho uzivatele
    /// Pokud se pres query parametr nespecifikuje uzivatel, vraci se kolekce statistik poslednich 50 tasku celeho systemu za posledni den
    /// Statistiky udavaji kolik casu dany task stravil ve fronte a ve workeru
    /// </summary>
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
    /// Vraci se kolekce kde polozka je pocet zalozenych tasku za den
    /// Vraci se zalozene tasky za poslednich 30 dni od zadaneho uzivatelu, pokud se uzivatel nespecifikuje, vrati se od tasky od vsech uzivatelu
    /// Musi se specifikovat zda se budou vracet vip nebo normalni tasky
    /// </summary>
    [Authorize(Policy = TaskLauncherPolicies.CanViewGraphsPolicy)]
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
