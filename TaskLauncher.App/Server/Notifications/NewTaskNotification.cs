using MediatR;
using Microsoft.AspNetCore.SignalR;
using TaskLauncher.App.Server.Hub;
using TaskLauncher.App.Server.Tasks;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Notifications;

public class NewTaskNotification : INotification
{
    public TaskModel Task { get; }

    public NewTaskNotification(TaskModel task)
    {
        Task = task;
    }
}

public class NewTaskHandler : INotificationHandler<NewTaskNotification>
{
    private readonly IHubContext<WorkerHub, IWorkerHub> workerHub;
    private readonly TaskCache cache;

    public NewTaskHandler(IHubContext<WorkerHub, IWorkerHub> workerHub, TaskCache cache)
    {
        this.workerHub = workerHub;
        this.cache = cache;
    }

    public async Task Handle(NewTaskNotification notification, CancellationToken cancellationToken)
    {
        var connections = cache.Where(i => i.Value.Id == Guid.Empty).Select(i => i.Key);
        await workerHub.Clients.Clients(connections).WakeUpWorkers();
    }
}