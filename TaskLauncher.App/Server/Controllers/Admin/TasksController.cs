using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.DAL;
using TaskLauncher.App.Server.Controllers.Base;

namespace TaskLauncher.App.Server.Controllers.Admin;

/// <summary>
/// Task kontroler ke kteremu ma pristup pouze admin
/// </summary>
public class TasksController : AdminODataController<TaskResponse>
{
    private readonly IMapper mapper;

    public TasksController(AppDbContext context, IMapper mapper) : base(context)
    {
        this.mapper = mapper;
    }

    /// <summary>
    /// Zobrazi vsechny tasky v systemu, muze se dotazovat pres protokol odata
    /// </summary>
    public override ActionResult<IQueryable<TaskResponse>> Get()
    {
        return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
    }

    /// <summary>
    /// Zobrazi detail tasku
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailResponse>> GetDetail([FromRoute] Guid id)
    {
        var payment = await context.Payments.IgnoreQueryFilters().Include(i => i.Task).ThenInclude(i => i.Events).SingleOrDefaultAsync(i => i.Task.Id == id);
        if (payment is not null)
        {
            var taskResponse = mapper.Map<TaskDetailResponse>(payment.Task);
            taskResponse.Payment = mapper.Map<SimplePaymentResponse>(payment);
            return Ok(taskResponse);
        }

        var task = await context.Tasks.IgnoreQueryFilters().Include(i => i.Events).SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();
        return Ok(mapper.Map<TaskDetailResponse>(task));
    }

    /// <summary>
    /// Smazani tasku
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaskAsync([FromRoute] Guid id)
    {
        var task = await context.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return BadRequest();

        context.Remove(task);
        await context.SaveChangesAsync();
        return Ok();
    }
}
