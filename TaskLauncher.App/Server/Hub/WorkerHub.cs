using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
    /// Spusteni tasku, na teto zprave posloucha worker
    /// </summary>
    Task StartTask(TaskModel model);

    /// <summary>
    /// Probuzeni workera
    /// </summary>
    Task WakeUpWorkers();
}

/// <summary>
/// Hub pro workery, worker posila aktualizace o stavu tasku, server prideluje praci
/// </summary>
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

    /// <summary>
    /// Pomocna funkce pridelujici task workerovi
    /// </summary>
    private async Task SendTask()
    {
        var scope = serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

        CancellationTokenSource cts = new();
        cts.CancelAfter(5000);
        try
        {
            var model = await balancer.GetNext(cts.Token);
            //task mohl byt smazan, ujisteni zda existuje
            while (!await taskService.TaskExists(model))
            {
                model = await balancer.GetNext(cts.Token);
            }
            //posilani tasku na workera
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

    /// <summary>
    /// Po tom co server odeslal wake up zpravu, worker si zazada o dalsi task
    /// </summary>
    public async Task RequestNewWork()
    {
        logger.LogInformation("Worker '{0}' is asking for work", Context.ConnectionId);
        await SendTask();
    }

    /// <summary>
    /// Aktualizace stavu tasku, worker zasila udalosti typu Ready, Running, FinishedSuccess, FinishedFailure
    /// </summary>
    public async Task TaskStatusUpdate(TaskModel model)
    {
        //update
        var scope = serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var eventModel = await taskService.UpdateTaskAsync(model);

        //update cache
        var cachedTask = cache[Context.ConnectionId];
        cachedTask!.State = model.State;

        if(eventModel is not null)
            await mediator.Publish(new TaskUpdateNotification(model, eventModel));

        //pokud to je hotovy
        if (model.State.TaskFinished())
        {
            //ukonci task
            await taskService.EndTaskAsync(model);
            logger.LogInformation("Worker '{0}' finished task '{1}'", Context.ConnectionId, model.Id);
            //novy task
            await SendTask();
        }
    }

    /// <summary>
    /// Pokud se task nestihl na workerovi dokoncit, ulozi se event a posle se notifikace uzivateli
    /// </summary>
    public async Task TaskTimeouted(TaskModel model)
    {
        var scope = serviceProvider.CreateScope();
        var updateTaskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var eventModel = await updateTaskService.UpdateTaskAsync(model);
        await updateTaskService.EndTaskAsync(model);

        if (eventModel is not null)
            await mediator.Publish(new TaskUpdateNotification(model, eventModel));

        var cachedTask = cache[Context.ConnectionId];
        cachedTask!.State = model.State;
        await SendTask();
    }

    /// <summary>
    /// Pokud se na workeru vyvola vyjimka, vykona se tento kod a tak se opet vlozi do fronty
    /// </summary>
    public async Task TaskCrashed(TaskModel model)
    {
        await UpdateAndNotifyWhenCrash(model);
        balancer.Enqueue("cancel", model);
    }

    /// <summary>
    /// Pri pripojeni workera na hub se ihned prideli prace
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        //connect
        logger.LogInformation("Worker connected: '{0}'", Context.ConnectionId);
        await base.OnConnectedAsync();
        //pridel task
        await SendTask();
    }

    /// <summary>
    /// Pri odpojeni workera se zkontroluje zda necekane neukoncil spojeni, pokud ano tak se task vlozi opet do fronty
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        //pokud task neni ukonceni, jedna se o crash (worker se necekane odpojil apod.)
        if (cache.TryGetValue(Context.ConnectionId, out var model) && model.Id != Guid.Empty)
        {
            if (model.State != TaskState.FinishedSuccess || model.State != TaskState.FinishedFailure || model.State != TaskState.Timeouted)
            {
                logger.LogInformation("Worker '{0}' crashed. Task '{1}' will be requeued ", Context.ConnectionId, model.Id);

                await UpdateAndNotifyWhenCrash(model);

                //znovu zarad do fronty
                balancer.Enqueue("cancel", model);
            }
        }
        logger.LogInformation("Worker disconnected: '{0}'", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Pomocna funkce pro aktualizaci stavu tasku a poslani notifikaci k uzivateli
    /// </summary>
    private async Task UpdateAndNotifyWhenCrash(TaskModel model)
    {
        var scope = serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

        model.State = TaskState.Crashed;
        var eventModel = await taskService.UpdateTaskAsync(model);
        await taskService.EndTaskAsync(model);

        if (eventModel is not null)
            await mediator.Publish(new TaskUpdateNotification(model, eventModel));

        model.State = TaskState.Created;
        eventModel = await taskService.UpdateTaskAsync(model);

        if (eventModel is not null)
            await mediator.Publish(new TaskUpdateNotification(model, eventModel));
    }
}
