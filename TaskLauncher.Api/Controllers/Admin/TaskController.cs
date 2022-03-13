using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Api.Controllers.Base;
using TaskLauncher.Api.DAL;

namespace TaskLauncher.Api.Controllers.Admin;

public class TaskController : AdminODataController<TaskResponse>
{
    private readonly IMapper mapper;

    public TaskController(AppDbContext context, IMapper mapper) : base(context)
    {
        this.mapper = mapper;
    }

    public override ActionResult<IQueryable<TaskResponse>> Get(string userId = "")
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
        }
        return Ok(context.Tasks.IgnoreQueryFilters().Where(i => i.UserId == userId).ProjectToType<TaskResponse>());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail(Guid id)
    {
        var task = await context.Tasks.IgnoreQueryFilters().Include(i => i.Events).SingleOrDefaultAsync(i => i.Id == id );
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }
}
