using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.Common.Models;

namespace TaskLauncher.Worker.Extensions;

/// <summary>
/// Pomocne metody pro odebirani/poslani zprav pomoci signalr
/// </summary>
public static class HubConnectionExtension
{
    public static IDisposable OnTaskStarted(this HubConnection connection, Func<TaskModel, Task> handler)
    => connection.On("StartTask", handler);

    public static IDisposable OnCancelTask(this HubConnection connection, Func<TaskModel, Task> handler)
        => connection.On("CancelTask", handler);

    public static IDisposable OnWakeUpMessage(this HubConnection connection, Func<Task> handler)
        => connection.On("WakeUpWorkers", handler);

    public static async Task InvokeTaskStatusChanged(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("TaskStatusUpdate", value);

    public static async Task InvokeRequestWork(this HubConnection connection)
        => await connection.InvokeAsync("RequestNewWork");

    public static async Task InvokeTaskTimeouted(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("TaskTimeouted", value);

    public static async Task InvokeTaskCrashed(this HubConnection connection, TaskModel value)
        => await connection.InvokeAsync("TaskCrashed", value);
}
