using Microsoft.AspNetCore.Components;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Models;
using System.Net.Http.Json;
using TaskLauncher.Common.Enums;
using Microsoft.AspNetCore.Components.Authorization;
using TaskLauncher.App.Client.Extensions;
using Radzen;
using static TaskLauncher.App.Client.Pages.Tasks.EditTaskDialog;

namespace TaskLauncher.App.Client.Pages.Tasks;

public partial class TaskDetail : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Inject]
    public DialogService DialogService { get; set; }

    [Inject]
    protected NavigationManager navigationManager { get; set; }

    [Inject]
    protected ServiceAddresses serviceAddresses { get; set; }

    [Inject]
    protected SignalRClient signalRClient { get; set; }

    [Inject]
    protected ApiClient Client { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    public TaskDetailResponse? Task { get; set; } = new();

    private string message = "";
    private bool isRunning = false;
    private bool isLoading = true;

    private IDisposable eventSubscription;

    protected override async Task OnParametersSetAsync()
    {
        var state = await authenticationStateTask;
        if(state.User.IsInRole("admin"))
            Task = await Client.GetFromJsonAsync<TaskDetailResponse>("api/admin/task/" + Id.ToString());
        else
            Task = await Client.GetFromJsonAsync<TaskDetailResponse>("api/task/" + Id.ToString());

        if (Task is null)
        {
            navigationManager.NavigateTo("tasks");
            return;
        }
        
        signalRClient.OnTaskUpdate += StatusChanged;
        eventSubscription = signalRClient.Connection.OnNewEvent(NewEvent);
        isLoading = false;
    }

    async Task UpdateAsync()
    {
        TaskEditDialogResult? result = await DialogService.OpenAsync<EditTaskDialog>("Edit task", new() { { "Task", Task } }, new() { Width = "500px", Height = "400px", Resizable = true, Draggable = true });
        if (result is not null)
        {
            var response = await Client.PutAsJsonAsync("api/task/" + Task.Id.ToString(), result.TaskUpdate);
            if(response.IsSuccessStatusCode)
            {
                Task.Name = result.TaskUpdate.Name;
                Task.Description = result.TaskUpdate.Description;
            }
        }
    }

    private void NewEvent(EventModel model)
    {
        if (Task.Events is null)
            Task.Events = new();

        Task.Events.Add(new EventResponse
        {
            Status = model.Status,
            Time = model.Time
        });
    }

    //aktualizace gui po prijmuti signalr zpravy
    private void StatusChanged(TaskModel model)
    {
        Console.WriteLine($"Status changed: {model.State} on task: {model.Id}");
        Task.ActualStatus = model.State;
        StateHasChanged();
    }

    private async Task CancelTask()
    {
        var tmp = await Client.DeleteAsync("api/task/" + Id.ToString());
        if(tmp.IsSuccessStatusCode)
            navigationManager.NavigateTo("tasks", true);
    }

    private void DownloadResultFile()
    {
        if(Task.ActualStatus == TaskState.FinishedSuccess || Task.ActualStatus == TaskState.Downloaded)
        {
            navigationManager.NavigateTo("api/task/file?taskId=" + Id.ToString(), true);
            Task.ActualStatus = TaskState.Downloaded;
        }
    }

    public void Dispose()
    {
        signalRClient.OnTaskUpdate -= StatusChanged;
        eventSubscription.Dispose();
    }
}
