using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
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
    /// Vrací všechny úlohy v systému, dotazuje přes odata
    /// </summary>
    [ProducesResponseType(typeof(List<TaskResponse>), 200)]
    [Produces("application/json")]
    [HttpGet]
    [EnableQuery]
    public ActionResult<IQueryable<TaskResponse>> Get()
    {
        return Ok(context.Tasks.IgnoreQueryFilters().ProjectToType<TaskResponse>());
    }

    /// <summary>
    /// Zobrazí detail jakékoli úlohy v systému
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(typeof(TaskDetailResponse), 200)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
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
    /// Smazaní úlohy
    /// </summary>
    /// <param name="id" example="f6195afa-168d-4a30-902e-f4c93af06acd">Id úlohy</param>
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTaskAsync([FromRoute] Guid id)
    {
        var task = await context.Tasks.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == id);
        if (task is null)
            return NotFound();

        context.Remove(task);
        await context.SaveChangesAsync();
        return Ok();
    }
}
