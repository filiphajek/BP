using Auth0.ManagementApi;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Responses;

namespace TaskLauncher.WebApp.Client.Pages;

public partial class Tasks
{
    [Inject]
    protected HttpClient client { get; set; }

    public List<TaskResponse> taskList { get; set; } = new();

    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        //test
        SpaManagementApiClient apiClient = new("localhost:5001/auth0api");
        var clients = (await apiClient.Users.GetAllAsync(new())).ToList();

        var tasks = await client.GetFromJsonAsync<List<TaskResponse>>("api/tasks");
        taskList.AddRange(tasks);
        isLoading = false;
    }
}
