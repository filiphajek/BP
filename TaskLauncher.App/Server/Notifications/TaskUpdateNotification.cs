using MediatR;
using Microsoft.AspNetCore.SignalR;
using TaskLauncher.App.Server.Hub;
using TaskLauncher.Common.Enums;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Server.Notifications;

/// <summary>
/// Notifikace na udalost aktualizace tasku
/// </summary>
public class TaskUpdateNotification : INotification
{
    public TaskModel Task { get; }
    public EventModel Event { get; }

    public TaskUpdateNotification(TaskModel task, EventModel ev)
    {
        Task = task;
        Event = ev;
    }
}

/// <summary>
/// Handler, ktery se vyvola po vygenerovani nove udalosti TaskUpdateNotification
/// </summary>
public class TaskUpdateHandler : INotificationHandler<TaskUpdateNotification>
{
    private readonly SignalRMemoryStorage userConnectionsStorage;
    private readonly IHubContext<UserHub, IUserHub> hubContext;

    public TaskUpdateHandler(SignalRMemoryStorage userConnectionsStorage, IHubContext<UserHub, IUserHub> hubContext)
    {
        this.userConnectionsStorage = userConnectionsStorage;
        this.hubContext = hubContext;
    }

    public async Task Handle(TaskUpdateNotification notification, CancellationToken cancellationToken)
    {
        var connections = userConnectionsStorage.GetConnections(notification.Task.UserId);
        if (notification.Task.State.TaskFinished())
            await hubContext.Clients.Clients(connections).TaskFinished(notification.Task);
        await hubContext.Clients.Clients(connections).SendEvent(notification.Event);
    }
}