using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Client.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnNotification(this HubConnection connection, Func<TaskModel, Task> handler)
        => connection.On("SendNotification", handler);

    public static IDisposable OnNotification(this HubConnection connection, Action<TaskModel> handler)
        => connection.On("SendNotification", handler);
}
