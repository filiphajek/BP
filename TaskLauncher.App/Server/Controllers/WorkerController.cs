using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskLauncher.App.Server.Controllers.Base;
using TaskLauncher.App.Server.Notifications;
using TaskLauncher.App.Server.Services;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Controllers;

/// <summary>
/// Kontroler vyhrazeny pro workery
/// </summary>
[Authorize(Policy = Constants.Policies.WorkerPolicy)]
public class WorkerController : BaseController
{
    private readonly IMediator mediator;
    private readonly ITaskService taskService;
    private readonly Balancer balacer;

    public WorkerController(IMediator mediator, ITaskService taskService, Balancer balacer, ILogger<WorkerController> logger) : base(logger)
    {
        this.mediator = mediator;
        this.taskService = taskService;
        this.balacer = balacer;
    }

    /// <summary>
    /// Přidělí se další úloha
    /// </summary>
    [ProducesResponseType(typeof(TaskModel), 200)]
    [Produces("application/json")]
    [HttpGet]
    public ActionResult<TaskModel> GetTask()
    {
        var tmp = balacer.GetNext();
        if(tmp is null)
            return NotFound();
        return Ok(tmp);
    }

    /// <summary>
    /// Zasílá se nová událost informujicí o stavu úlohy
    /// </summary>
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EventModel), 200)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<ActionResult<EventModel>> CreateNewTaskEvent(TaskModel model)
    {
        var eventModel = await taskService.UpdateTaskAsync(model);
        if (eventModel is not null)
            await mediator.Publish(new TaskUpdateNotification(model, eventModel));

        //pokud task skoncil, zaznamenej to
        if (model.State.TaskFinished())
        {
            await taskService.EndTaskAsync(model);
        }

        //pokud task zhavaroval, znovu ho dej do fronty
        if (model.State == TaskState.Crashed)
        {
            model.State = TaskState.Created;
            eventModel = await taskService.UpdateTaskAsync(model);

            if (eventModel is not null)
                await mediator.Publish(new TaskUpdateNotification(model, eventModel));

            balacer.Enqueue("cancel", model);
        }

        return Ok(eventModel);
    }
}
