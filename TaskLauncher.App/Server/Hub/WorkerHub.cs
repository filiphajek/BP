using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.App.DAL;
using TaskLauncher.App.DAL.Entities;
using TaskLauncher.App.Server.Tasks;
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
    Task IsWorking();
}

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //Policy = "worker"
public class WorkerHub : Hub<IWorkerHub>
{
    private readonly ILogger<WorkerHub> logger;
    private readonly IMapper mapper;
    private readonly TaskCache cache;
    private readonly Balancer balancer;
    private readonly IServiceProvider provider;
    private readonly IHubContext<UserHub, IUserHub> userHubContext;
    private readonly SignalRMemoryStorage userConnectionsStorage;

    public WorkerHub(ILogger<WorkerHub> logger, 
        IMapper mapper,
        TaskCache cache, 
        Balancer balancer, 
        IServiceProvider provider, 
        IHubContext<UserHub, IUserHub> userHubContext, 
        SignalRMemoryStorage userConnectionsStorage)
    {
        this.logger = logger;
        this.mapper = mapper;
        this.cache = cache;
        this.balancer = balancer;
        this.provider = provider;
        this.userHubContext = userHubContext;
        this.userConnectionsStorage = userConnectionsStorage;
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
            balancer.ClientsWithoutWork = true;
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task<EventModel> UpdateDatabase(TaskModel model)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.Tasks.IgnoreQueryFilters().SingleAsync(i => i.Id == model.Id);
        task.ActualStatus = model.State;
        dbContext.Update(task);
        var ev = new EventEntity() { Status = model.State, Task = task, Time = DateTime.Now, UserId = model.UserId };
        await dbContext.Events.AddAsync(ev);
        await dbContext.SaveChangesAsync();
        
        var tmp = mapper.Map<EventModel>(ev);
        tmp.TaskId = model.Id;
        return tmp;
    }

    public async Task GiveMeWork()
    {
        logger.LogInformation("Worker '{0}' is asking for work", Context.ConnectionId);
        await SendTask();
    }

    public async Task TaskStatusUpdate(TaskModel model)
    {
        //update - lze udelat i jako http endpoint
        var eventModel = await UpdateDatabase(model);

        if (model.UserId is not null)
        {
            var connections = userConnectionsStorage.GetConnections(model.UserId);
            await userHubContext.Clients.Clients(connections).SendEvent(eventModel);

            if (model.State == Common.Enums.TaskState.FinishedSuccess)
                await userHubContext.Clients.Clients(connections).Notify(model);
        }

        //update cache
        var cachedTask = cache[Context.ConnectionId];
        cachedTask!.State = model.State;

        //toto mit jako novou message, mit update message a pak send new work message
        if (model.State == Common.Enums.TaskState.FinishedSuccess)
        {
            logger.LogInformation("Worker '{0}' finished task '{1}'", Context.ConnectionId, model.Id);
            //take new task
            await SendTask();
        }
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
        //is task finished?
        if (cache.TryGetValue(Context.ConnectionId, out var model))
        {
            if (model.State != Common.Enums.TaskState.FinishedSuccess)
            {
                logger.LogInformation("Worker '{0}' crashed. Task '{1}' will be requeued ", Context.ConnectionId, model.Id);
                model.State = Common.Enums.TaskState.Cancelled;
                //do db ulozit event zhavarovano
                //await UpdateDatabase(model);
                balancer.Enqueue("cancel", model);
            }
        }
        logger.LogInformation("Worker disconnected: '{0}'", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
