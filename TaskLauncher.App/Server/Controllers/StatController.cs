using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.Authorization;

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

    [Authorize(Policy = TaskLauncherPolicies.UserPolicy)]
    public async Task<ActionResult<UserStatResponse>> GetUserStats()
    {
        var tmp = await dbContext.Stats.SingleOrDefaultAsync();
        if(tmp is null)
            return BadRequest();
        return Ok(mapper.Map<UserStatResponse>(tmp));
    }

    [Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserStatResponse>> GetUserStats([FromRoute] string userId)
    {
        var tmp = await dbContext.Stats.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.UserId == userId);
        if (tmp is null)
            return BadRequest();
        return Ok(mapper.Map<UserStatResponse>(tmp));
    }

    [Authorize(Policy = TaskLauncherPolicies.AdminPolicy)]
    [HttpGet("all")]
    public async Task<ActionResult<UserStatResponse>> GetAllStats()
    {
        UserStatResponse stat = new()
        {
            AllTaskCount = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.AllTaskCount),
            FinishedTaskCount = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.FinishedTaskCount),
            SuccessTasks = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.SuccessTasks),
            CrashedTasks = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.CrashedTasks),
            TimeoutedTasks = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.TimeoutedTasks),
            FailedTasks = await dbContext.Stats.IgnoreQueryFilters().SumAsync(i => i.FailedTasks)
        };
        return Ok(stat);
    }
}