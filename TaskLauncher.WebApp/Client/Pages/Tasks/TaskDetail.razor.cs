using Microsoft.AspNetCore.Components;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Configuration;
using TaskLauncher.Common.Models;
using System.Net.Http.Json;
using TaskLauncher.Common.Enums;
using Microsoft.AspNetCore.Components.Authorization;
using TaskLauncher.WebApp.Client.Extensions;

namespace TaskLauncher.WebApp.Client.Pages.Tasks;

public partial class TaskDetail : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Inject]
    protected NavigationManager navigationManager { get; set; }

    private HttpClient client;

    [Inject]
    protected ServiceAddresses serviceAddresses { get; set; }

    [Inject]
    protected SignalRClient signalRClient { get; set; }

    [Parameter]
    public Guid Id { get; set; }

    public TaskDetailResponse? Task { get; set; } = new();

    private string message = "";
    private bool isRunning = false;
    private bool isLoading = true;

    protected override async Task OnParametersSetAsync()
    {
        client = HttpClientFactory.CreateApiClient();
        var state = await authenticationStateTask;
        if(state.User.IsInRole("admin"))
            Task = await client.GetFromJsonAsync<TaskDetailResponse>("api/admin/task/" + Id.ToString());
        else
            Task = await client.GetFromJsonAsync<TaskDetailResponse>("api/task/" + Id.ToString());

        if (Task is null)
        {
            navigationManager.NavigateTo("tasks");
            return;
        }
        signalRClient.OnTaskUpdate += StatusChanged;
        isLoading = false;
    }

    //aktualizace gui po prijmuti signalr zpravy
    private void StatusChanged(TaskModel model)
    {
        Console.WriteLine($"Status changed: {model.State} on task: {model.Id}");
        /*Task.ActualStatus = model.State;
        if (model.State == TaskState.InQueue)
        {
            Task.Start = model.Time;
            Task.End = null;
        }
        if (model.State == TaskState.Finished)
        {
            Task.End = model.Time;
            isRunning = false;
        }*/
        StateHasChanged();
    }

    private async Task CancelTask()
    {
        var tmp = await client.DeleteAsync("api/task/" + Id.ToString());
        if(tmp.IsSuccessStatusCode)
            navigationManager.NavigateTo("tasks", true);
    }

    private void DownloadResultFile()
    {
        if(Task.ActualStatus == TaskState.Finished)
        {
            navigationManager.NavigateTo("api/file/" + Id.ToString(), true);
        }
    }

    public void Dispose()
    {
        signalRClient.OnTaskUpdate -= StatusChanged;
    }
}
