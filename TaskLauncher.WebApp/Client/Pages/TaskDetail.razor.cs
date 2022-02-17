using Microsoft.AspNetCore.Components;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using TaskLauncher.WebApp.Client.Extensions;
using TaskLauncher.Common.Models;
using System.Net.Http.Json;
using TaskLauncher.Common.Enums;

namespace TaskLauncher.WebApp.Client.Pages;

public partial class TaskDetail
{
    [Inject]
    protected NavigationManager navigationManager { get; set; }

    [Inject]
    protected HttpClient client { get; set; }

    [Inject]
    protected ServiceAddresses serviceAddresses { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    public TaskDetailResponse? Task { get; set; } = new();

    private HubConnection hubConnection;

    private string message = "";
    private bool isRunning = false;
    private bool isLoading = true;

    protected override async Task OnParametersSetAsync()
    {
        Task = await client.GetFromJsonAsync<TaskDetailResponse>("api/task/" + Id.ToString());
        if (Task is null)
        {
            navigationManager.NavigateTo("tasks");
            return;
        }
        hubConnection = new HubConnectionBuilder()
            .WithUrl(serviceAddresses.HubAddress)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.OnTaskStatusChanged(StatusChanged);
        await hubConnection.StartAsync();
        isLoading = false;
    }

    //aktualizace gui po prijmuti signalr zpravy
    private void StatusChanged(TaskModel model)
    {
        Console.WriteLine($"status changed: {model.State}");
        Task.Status = model.State;
        if (model.State == TaskState.InQueue)
        {
            Task.Start = model.Time;
            Task.End = null;
        }
        if (model.State == TaskState.Finished)
        {
            Task.End = model.Time;
            isRunning = false;
        }
        StateHasChanged();
    }

    private async Task StartTask()
    {
        isRunning = true;
        await hubConnection.InvokeStartTask(new() { TaskId = Task.Id });
    }

    private async Task RemoveTask()
    {
        var tmp = await client.DeleteAsync("api/task/" + Id.ToString());
        if(tmp.IsSuccessStatusCode)
            navigationManager.NavigateTo("tasks", true);
    }

    private void DownloadResultFile()
    {
        if(Task.Status == TaskState.Finished)
        {
            navigationManager.NavigateTo("api/file/" + Id.ToString(), true);
        }
    }
}
