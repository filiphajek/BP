using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Worker.Extensions;

public static class HubConnectionExtension
{
    public static IDisposable OnTaskStarted(this HubConnection connection, Func<TaskModel, Task> handler)
    => connection.On("StartTask", handler);

    public static IDisposable OnCancelTask(this HubConnection connection, Action<TaskModel> handler)
        => connection.On("CancelTask", handler);

    public static IDisposable OnIsWorking(this HubConnection connection, Func<Task> handler)
        => connection.On("IsWorking", handler);

    public static async Task InvokeTaskStatusChanged(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("TaskStatusUpdate", value);

    public static async Task InvokeGiveMeWork(this HubConnection connection)
        => await connection.InvokeAsync("GiveMeWork");
}
