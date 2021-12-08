using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Client.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnTaskStatusChangedAsync(this HubConnection connection, Func<TaskModel, Task> handler)
        => connection.On("TaskStatusChanged", handler);

    public static IDisposable OnTaskStatusChanged(this HubConnection connection, Action<TaskModel> handler)
    => connection.On("TaskStatusChanged", handler);

    public static async Task InvokeStartTask(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("StartTask", value);
}