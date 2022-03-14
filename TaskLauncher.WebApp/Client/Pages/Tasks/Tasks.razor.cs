using GridBlazor;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Primitives;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.WebApp.Client.Authentication;
using TaskLauncher.WebApp.Client.Extensions;

namespace TaskLauncher.WebApp.Client.Pages.Tasks;

public partial class Tasks
{
    [Inject]
    public HostAuthenticationStateProvider StateProvider { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Parameter]
    public string Id { get; set; }

    private HttpClient httpClient;
    
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private CGrid<TaskResponse> grid;
    private Task task;

    Action<IGridColumnCollection<TaskResponse>> columns = c =>
    {
        c.Add(o => o.Id).Encoded(false).Sanitized(false).RenderValueAs(o => $"<a href='tasks/{o.Id}'>Detail</a>");
        c.Add(o => o.Description);
        c.Add(o => o.ActualStatus);
        c.Add(o => o.TaskFile);
    };

    async Task GetTasks(string path)
    {
        string url = NavigationManager.BaseUri + path;
        var query = new QueryDictionary<StringValues>();

        var client = new GridODataClient<TaskResponse>(httpClient, url, query, false, "taskGrid", columns, 10)
            .ChangePageSize(true)
            .Sortable()
            .Filterable()
            .WithMultipleFilters()
            .WithGridItemsCount();

        grid = client.Grid;
        task = client.UpdateGrid();
        await task;
    }

    protected async override Task OnParametersSetAsync()
    {
        httpClient = HttpClientFactory.CreateApiClient();
        if (string.IsNullOrEmpty(Id))
        {
            await GetTasks("odata/user/task");
        }
        else
        {
            var state = await authenticationStateTask;
            if (!state.User.IsInRole("admin"))
            {
                NavigationManager.NavigateTo("tasks", true);
                return;
            }
            await GetTasks($"odata/admin/task?userId={Id}");
        }
    }
}
