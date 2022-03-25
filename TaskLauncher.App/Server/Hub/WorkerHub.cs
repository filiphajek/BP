using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Notifications;
using TaskLauncher.App.Server.Services;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Hub;

public interface IWorkerHub
{
    /// <summary>
    /// Spusteni tasu
    /// </summary>
    Task StartTask(TaskModel model);

    /// <summary>
    /// Zruseni tasku
    /// </summary>
    Task CancelTask(TaskModel model);

    /// <summary>
    /// Dotazeni se workeru zda neco dela
    /// </summary>
    Task WakeUpWorkers();
}

[Authorize(Policy = TaskLauncherPolicies.LauncherPolicy)]
public class WorkerHub : Hub<IWorkerHub>
{
    private readonly ILogger<WorkerHub> logger;
    private readonly TaskCache cache;
    private readonly Balancer balancer;
    private readonly IServiceProvider serviceProvider;
    private readonly IMediator mediator;

    public WorkerHub(ILogger<WorkerHub> logger,
        TaskCache cache,
        Balancer balancer,
        IMediator mediator, 
        IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.cache = cache;
        this.balancer = balancer;
        this.mediator = mediator;
        this.serviceProvider = serviceProvider;
    }

    private async Task SendTask()
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(7000);
        try
        {
            var model = await balancer.GetNext(cts.Token);
            await Clients.Caller.StartTask(model);
            cache.AddOrSet(Context.ConnectionId, model);
            logger.LogInformation("Worker '{0}' started new task '{1}'", Context.ConnectionId, model.Id);
        }
        catch (OperationCanceledException)
        {
            cache.AddOrSet(Context.ConnectionId, new() { Id = Guid.Empty});
        }
        finally
        {
            cts.Dispose();
        }
    }

    public async Task RequestNewWork()
    {
        logger.LogInformation("Worker '{0}' is asking for work", Context.ConnectionId);
        await SendTask();
    }

    public async Task TaskStatusUpdate(TaskModel model)
    {
        //update
        var scope = serviceProvider.CreateScope();
        var updateTaskService = scope.ServiceProvider.GetRequiredService<IUpdateTaskService>();
        var eventModel = await updateTaskService.UpdateTaskAsync(model);

        //update cache
        var cachedTask = cache[Context.ConnectionId];
        cachedTask!.State = model.State;

        await mediator.Publish(new TaskUpdateNotification(model, eventModel));

        //pokud to je hotovy
        if (model.State.TaskFinished())
        {
            //ukonci task
            await updateTaskService.EndTaskAsync(model);
            logger.LogInformation("Worker '{0}' finished task '{1}'", Context.ConnectionId, model.Id);
            //novy task
            await SendTask();
        }
    }

    public async Task TaskTimeouted(TaskModel model)
    {
        var scope = serviceProvider.CreateScope();
        var updateTaskService = scope.ServiceProvider.GetRequiredService<IUpdateTaskService>();
        var eventModel = await updateTaskService.UpdateTaskAsync(model);
        await mediator.Publish(new TaskUpdateNotification(model, eventModel));
        await SendTask();
    }

    public async Task TaskCrashed(TaskModel model)
    {
        var scope = serviceProvider.CreateScope();
        var updateTaskService = scope.ServiceProvider.GetRequiredService<IUpdateTaskService>();
        model.State = TaskState.Crashed;
        await updateTaskService.UpdateTaskAsync(model);
        await updateTaskService.EndTaskAsync(model);
        model.State = TaskState.Created;
        await updateTaskService.UpdateTaskAsync(model);
        balancer.Enqueue("cancel", model);
    }

    public override async Task OnConnectedAsync()
    {
        //connect
        logger.LogInformation("Worker connected: '{0}'", Context.ConnectionId);
        await base.OnConnectedAsync();
        //take task
        await SendTask();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        //pokud task neni ukonceni, jedna se o crash (worker se necekane odpojil apod.)
        if (cache.TryGetValue(Context.ConnectionId, out var model))
        {
            if (model.State != TaskState.FinishedSuccess || model.State != TaskState.FinishedFailure || model.State != TaskState.Timeouted)
            {
                var scope = serviceProvider.CreateScope();
                var updateTaskService = scope.ServiceProvider.GetRequiredService<IUpdateTaskService>();

                logger.LogInformation("Worker '{0}' crashed. Task '{1}' will be requeued ", Context.ConnectionId, model.Id);
                model.State = TaskState.Crashed;
                await updateTaskService.UpdateTaskAsync(model);
                await updateTaskService.EndTaskAsync(model);
                model.State = TaskState.Created;
                await updateTaskService.UpdateTaskAsync(model);
                balancer.Enqueue("cancel", model);
            }
        }
        logger.LogInformation("Worker disconnected: '{0}'", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
