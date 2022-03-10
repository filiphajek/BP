using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskLauncher.Api.DAL;
using TaskLauncher.Common.Models;
using TaskLauncher.WebApp.Server.Tasks;

namespace TaskLauncher.WebApp.Server.Hub;

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

public class BackgroundTask
{
    private Task timerTask;
    private readonly CancellationTokenSource _cts = new();

    public void Start(TimeSpan timeSpan)
    {
        async Task DoStart()
        {
            try
            {
                var timer = new PeriodicTimer(timeSpan);

                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    Console.WriteLine(DateTime.UtcNow);
                }
            }
            catch (OperationCanceledException e)
            {
            }
        }

        timerTask = DoStart();
    }

    public async Task StopAsync()
    {
        _cts.Cancel();

        await timerTask;

        _cts.Dispose();

        Console.WriteLine("BackgroundTask cancelled");
    }
}


[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] //Policy = "worker"
public class WorkerHub : Hub<IWorkerHub>
{
    private readonly ILogger<WorkerHub> logger;
    private readonly TaskCache cache;
    private readonly Balancer balancer;
    private readonly IServiceProvider provider;
    private readonly IHubContext<UserHub, IUserHub> userHubContext;
    private readonly SignalRMemoryStorage userConnectionsStorage;

    public WorkerHub(ILogger<WorkerHub> logger, TaskCache cache, Balancer balancer, IServiceProvider provider, IHubContext<UserHub, IUserHub> userHubContext, SignalRMemoryStorage userConnectionsStorage)
    {
        this.logger = logger;
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
        catch(OperationCanceledException)
        {
            balancer.ClientsWithoutWork = true;
            //balancer.Enqueue("vip", new() { Id = Guid.NewGuid(), State = Common.Enums.TaskState.Created, Time = DateTime.Now, TaskFilePath = "vip" });
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task UpdateDatabase(TaskModel model)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = await dbContext.Tasks.SingleAsync(i => i.Id == model.Id);
        task.ActualStatus = model.State;
        dbContext.Update(task);
        var ev = await dbContext.Events.AddAsync(new() { Status = model.State, Task = task, Time = DateTime.Now, UserId = model.UserId });
        await dbContext.SaveChangesAsync();
    }

    public async Task GiveMeWork()
    {
        logger.LogInformation("Worker '{0}' is asking for work", Context.ConnectionId);
        await SendTask();
    }

    public async Task TaskStatusUpdate(TaskModel model)
    {
        if(model.UserId is not null)
        {
            var connections = userConnectionsStorage.GetConnections(model.UserId);
            await userHubContext.Clients.Clients(connections).Notify(model);
        }
        //update - lze udelat i jako http endpoint
        //await UpdateDatabase(model);

        //update cache
        var cachedTask = cache[Context.ConnectionId];
        cachedTask!.State = model.State;

        //toto mit jako novou message, mit update message a pak send new work message
        if (model.State == Common.Enums.TaskState.Finished)
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
        if(cache.TryGetValue(Context.ConnectionId, out var model))
        {
            if(model.State != Common.Enums.TaskState.Finished)
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
