using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnTaskFinished(this HubConnection connection, Action<TaskModel> handler)
        => connection.On("TaskFinished", handler);

    public static IDisposable OnNewEvent(this HubConnection connection, Action<EventModel> handler)
        => connection.On("SendEvent", handler);
}
