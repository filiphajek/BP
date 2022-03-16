using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.Admin;

public class TaskController : AdminODataController<TaskResponse>
{
    private readonly IMapper mapper;

    public TaskController(AppDbContext context, IMapper mapper) : base(context)
    {
        this.mapper = mapper;
    }

    public override ActionResult<IQueryable<TaskResponse>> Get()
    {
        return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail(Guid id)
    {
        var task = await context.Tasks.IgnoreQueryFilters().Include(i => i.Events).SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }
}
