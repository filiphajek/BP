using Microsoft.AspNetCore.SignalR;
using TaskLauncher.Common.Models;
using TaskLauncher.WebApp.Server.Hub;

namespace TaskLauncher.WebApp.Server.Services;

public class NotificationService
{
    private readonly SignalRMemoryStorage userConnectionsStorage;
    private readonly IHubContext<UserHub> hubContext;

    public NotificationService(SignalRMemoryStorage userConnectionsStorage, IHubContext<UserHub> hubContext)
    {
        this.userConnectionsStorage = userConnectionsStorage;
        this.hubContext = hubContext;
    }

    public void Notify(TaskModel model, string userId)
    {
        hubContext.Clients.Clients(userConnectionsStorage.GetConnections(userId)).SendAsync("Notify", model);
    }
}