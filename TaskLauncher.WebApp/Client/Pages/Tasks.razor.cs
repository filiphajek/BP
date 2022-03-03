using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;

namespace TaskLauncher.WebApp.Client.Pages;

public partial class Tasks
{
    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Inject]
    protected HttpClient client { get; set; }

    public List<TaskResponse> taskList { get; set; } = new();

    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var authState = await authenticationStateTask;
        if (authState.User.IsInRole("user") && string.IsNullOrEmpty(Id))
        {
            var tasks = await client.GetFromJsonAsync<List<TaskResponse>>("api/tasks");
            taskList.AddRange(tasks);
            isLoading = false;
        }
    }

    protected async override Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
            return;
        var tasks = await client.GetFromJsonAsync<List<TaskResponse>>($"api/{Id}/tasks");
        taskList.AddRange(tasks);
        isLoading = false;
    }
}
