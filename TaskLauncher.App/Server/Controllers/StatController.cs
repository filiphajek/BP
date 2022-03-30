using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.App.Server.Controllers;

public class StatController : BaseController
{
    private readonly IMapper mapper;
    private readonly AppDbContext dbContext;

    public StatController(IMapper mapper, AppDbContext dbContext, ILogger<StatController> logger) : base(logger)
    {
        this.mapper = mapper;
        this.dbContext = dbContext;
    }

    [Authorize(Policy = TaskLauncherPolicies.CanViewGraphsPolicy)]
    [HttpGet]
    public async Task<ActionResult<List<UserStatResponse>>> GetUserStats(string? userId = null)
    {
        if(!string.IsNullOrEmpty(userId))
        {
            if (!User.IsInRole(TaskLauncherRoles.Admin))
                return Unauthorized();

            var stats = await dbContext.Stats.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync();
            if (stats is null)
                return BadRequest();
            return Ok(stats.Select(mapper.Map<UserStatResponse>));
        }
        var userStats = await dbContext.Stats.ToListAsync();
        if (userStats is null)
            return BadRequest();
        return Ok(userStats.Select(mapper.Map<UserStatResponse>));
    }

    [Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
    [HttpGet("all")]
    public async Task<ActionResult<List<UserStatResponse>>> GetAllStats(bool? isVip = null)
    {
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

    [Authorize(Policy = TaskLauncherPolicies.CanViewGraphsPolicy)]
    [HttpGet("times")]
    public async Task<ActionResult<List<TaskStatResponse>>> GetUserTasksTimes(string? userId = null)
    {
        var minDate = DateTime.Now.AddDays(-1);
        List<TaskStatResponse> list = new();
        
        if (!string.IsNullOrEmpty(userId))
        {
            if (!User.IsInRole(TaskLauncherRoles.Admin))
                return Unauthorized();

            foreach (var task in await dbContext.Tasks
                .IgnoreQueryFilters()
                .Include(i => i.Events)
                .Where(i => i.UserId == userId)
                .Where(i => i.CreationDate >= minDate)
                .OrderByDescending(i => i.CreationDate)
                .Take(30)
                .ToListAsync())
            {
                var (TimeInQueue, CpuTime) = GetTimeStats(task);
                list.Add(new(task.IsPriority, task.Name, TimeInQueue, CpuTime));
            }
            return Ok(list);
        }
        foreach (var task in await dbContext.Tasks
            .Include(i => i.Events)
            .Where(i => i.CreationDate >= minDate)
            .OrderByDescending(i => i.CreationDate)
            .Take(30)
            .ToListAsync())
        {
            var (TimeInQueue, CpuTime) = GetTimeStats(task);
            list.Add(new(task.IsPriority, task.Name, TimeInQueue, CpuTime));
        }
        return Ok(list);
    }

    [AllowAnonymous]
    [Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
    [HttpGet("alltimes")]
    public async Task<ActionResult<List<TaskStatResponse>>> GetAllTaskTimes()
    {
        var minDate = DateTime.Now.AddDays(-1);
        List<TaskStatResponse> list = new();

        foreach (var task in await dbContext.Tasks
            .IgnoreQueryFilters()
            .Include(i => i.Events)
            .Where(i => i.CreationDate >= minDate)
            .OrderByDescending(i => i.CreationDate)
            .Take(30)
            .ToListAsync())
        {
            var (TimeInQueue, CpuTime) = GetTimeStats(task);
            list.Add(new(task.IsPriority, task.Name, TimeInQueue, CpuTime));
        }
        return Ok(list);
    }

    [Authorize(Policy = TaskLauncherPolicies.CanViewGraphsPolicy)]
    [HttpGet("daycount")]
    public async Task<ActionResult<List<DayTaskCountResponse>>> GetTaskCountInDays(bool vip, string? userId = null)
    {
        var minDate = DateTime.Now.AddDays(-30);
        if (!string.IsNullOrEmpty(userId))
        {
            if (!User.IsInRole(TaskLauncherRoles.Admin))
                return Unauthorized();

            var tmp1 = await dbContext.Tasks.IgnoreQueryFilters()
                .Where(i => i.IsPriority == vip)
                .Where(i => i.UserId == userId)
                .Where(i => i.CreationDate >= minDate)
                .GroupBy(i => i.CreationDate.Date)
                .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
                .ToListAsync();
            return Ok(tmp1);
        }
        var tmp2 = await dbContext.Tasks
            .Where(i => i.IsPriority == vip)
            .Where(i => i.CreationDate >= minDate)
            .GroupBy(i => i.CreationDate.Date)
            .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
            .ToListAsync();
        return Ok(tmp2);
    }

    [Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
    [HttpGet("dayallcount")]
    public async Task<ActionResult<List<DayTaskCountResponse>>> GetAllTaskCountInDays()
    {
        var minDate = DateTime.Now.AddDays(-30);
        var tmp = await dbContext.Tasks.IgnoreQueryFilters()
            .Where(i => i.CreationDate >= minDate)
            .GroupBy(i => i.CreationDate.Date)
            .Select(x => new DayTaskCountResponse(x.Count(), x.Key))
            .ToListAsync();
        return Ok(tmp);
    }

    private static (TimeSpan TimeInQueue, TimeSpan CpuTime) GetTimeStats(TaskEntity task)
    {
        var timeInQueue = TimeSpan.Zero;
        var cpuTime = TimeSpan.Zero;

        var events = task.Events.OrderBy(i => i.Time);
        var tmpTime = TimeSpan.Zero;
        var lastTime = DateTime.MinValue;
        foreach (var ev in events)
        {
            if (ev.Status == TaskState.Created)
            {
                lastTime = ev.Time;
                tmpTime = TimeSpan.Zero;
                continue;
            }

            tmpTime += ev.Time - lastTime;
            lastTime = ev.Time;
            if (ev.Status == TaskState.Ready)
            {
                timeInQueue += tmpTime;
                tmpTime = TimeSpan.Zero;
            }
            if (ev.Status == TaskState.FinishedSuccess || ev.Status == TaskState.FinishedFailure || ev.Status == TaskState.Crashed || ev.Status == TaskState.Timeouted)
            {
                cpuTime += tmpTime;
                tmpTime = TimeSpan.Zero;
            }
        }
        return new(timeInQueue, cpuTime);
    }
}