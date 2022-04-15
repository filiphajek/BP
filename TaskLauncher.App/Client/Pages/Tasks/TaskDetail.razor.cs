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
using Microsoft.AspNetCore.JsonPatch;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Common;

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

    bool isAdmin = false;

    protected override async Task OnParametersSetAsync()
    {
        var state = await authenticationStateTask;
        isAdmin = state.User.IsInRole(Constants.Roles.Admin);
        if (isAdmin)
            Task = await Client.GetFromJsonAsync<TaskDetailResponse>("api/admin/tasks/" + Id.ToString());
        else
            Task = await Client.GetFromJsonAsync<TaskDetailResponse>("api/tasks/" + Id.ToString());

        if (Task is null)
        {
            navigationManager.NavigateTo("tasks");
            return;
        }
        
        signalRClient.OnTaskFinished += StatusChanged;
        eventSubscription = signalRClient.Connection.OnNewEvent(NewEvent);
        isLoading = false;
    }

    async Task UpdateAsync()
    {
        if (isAdmin)
            return;

        TaskEditDialogResult? result = await DialogService.OpenAsync<EditTaskDialog>("Edit task", new() { { "Task", Task } }, new() { Width = "500px", Height = "400px", Resizable = true, Draggable = true });
        if (result is not null)
        {
            var patchDoc = new JsonPatchDocument<TaskUpdateRequest>();
            patchDoc.Replace(e => e.Description, result.TaskUpdate.Description);
            patchDoc.Replace(e => e.Name, result.TaskUpdate.Name);
            var response = await Client.PatchAsJsonAsync("api/tasks/" + Task.Id.ToString(), patchDoc);
            if(response.IsSuccessStatusCode)
            {
                Task.Name = result.TaskUpdate.Name;
                Task.Description = result.TaskUpdate.Description;
            }
        }
    }

    private void NewEvent(EventModel model)
    {
        if (model.TaskId != Task!.Id)
            return;

        if (Task.Events is null)
            Task.Events = new();

        Task.Events.Add(new EventResponse
        {
            Status = model.Status,
            Time = model.Time
        });
        StateHasChanged();
    }

    private void StatusChanged(TaskModel model)
    {
        Console.WriteLine($"Status changed: {model.State} on task: {model.Id}");
        Task.ActualStatus = model.State;
        StateHasChanged();
    }

    private async Task CloseTask()
    {
        var tmp = await Client.PostAsJsonAsync($"api/tasks/{Id}/close", new {});
        if (tmp.IsSuccessStatusCode)
            navigationManager.NavigateTo("/tasks", true);
    }

    private async Task CancelTask()
    {
        var tmp = await Client.PostAsJsonAsync($"api/tasks/{Id}/cancel", new {});
        if(tmp.IsSuccessStatusCode)
        {
            var response = await tmp.Content.ReadFromJsonAsync<EventResponse>();
            Task.ActualStatus = response.Status;
            Task.Events.Add(response);
        }
    }

    private async Task RestartTask()
    {
        var tmp = await Client.PostAsJsonAsync($"api/tasks/{Id}/restart" + Id.ToString(), new { });
        if (tmp.IsSuccessStatusCode)
        {
            var response = await tmp.Content.ReadFromJsonAsync<EventResponse>();
            Task.ActualStatus = response.Status;
            Task.Events.Add(response);
        }
    }

    private async Task DeleteTask()
    {
        if (isAdmin)
        {
            var tmp1 = await Client.DeleteAsync("api/admin/tasks/" + Id.ToString());
            if (tmp1.IsSuccessStatusCode)
                navigationManager.NavigateTo("/queue");
            return;
        }
        var tmp2 = await Client.DeleteAsync("api/tasks/" + Id.ToString());
        if (tmp2.IsSuccessStatusCode)
            navigationManager.NavigateTo("/tasks");
    }

    private void DownloadResultFile()
    {
        if(Task.ActualStatus == TaskState.FinishedSuccess || Task.ActualStatus == TaskState.FinishedFailure || Task.ActualStatus == TaskState.Downloaded)
        {
            navigationManager.NavigateTo($"api/tasks/{Id}/file", true);
            if (!isAdmin && Task.ActualStatus != TaskState.Downloaded)
            {
                Task.ActualStatus = TaskState.Downloaded;
                Task.Events.Add(new() { Description = "", Status = TaskState.Downloaded, Time = DateTime.Now });
            }
        }
    }

    public void Dispose()
    {
        signalRClient.OnTaskFinished -= StatusChanged;
        eventSubscription.Dispose();
    }
}
