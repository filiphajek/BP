using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.ContainerLauncher.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnTaskStart(this HubConnection connection, Func<TaskModel, Task> handler)
        => connection.On("StartTask", handler);

    public static async Task InvokeTaskStatusChanged(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("TaskStatusChanged", value);
}
