using GridBlazor;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Primitives;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Pages.Tasks;

public partial class Tasks : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Parameter]
    public string Id { get; set; }

    [Inject]
    protected SignalRClient signalRClient { get; set; }

    [Inject]
    protected ApiClient Client { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private CGrid<TaskResponse> grid;
    private Task task;

    Action<IGridColumnCollection<TaskResponse>> columns = c =>
    {
        c.Add(o => o.Name).Encoded(false).Sanitized(false).RenderValueAs(o => $"<a href='tasks/{o.Id}'>{o.Name}</a>").Sortable(true).Filterable(true);
        c.Add(o => o.Description).RenderValueAs(o => o.Description.Length > 50 ? o.Description[..50] + " ..." : o.Description).Filterable(true);
        c.Add(o => o.CreationDate).Titled("Creation date").Sortable(true).Filterable(true);
        c.Add(o => o.ActualStatus).Titled("Status").Sortable(true).Filterable(true);
        c.Add().RenderComponentAs(typeof(Components.ColumnTaskStatus));
    };

    async Task GetTasks(string path)
    {
        string url = NavigationManager.BaseUri + path;
        var query = new QueryDictionary<StringValues>();

        var client = new GridODataClient<TaskResponse>(Client, url, query, false, "taskGrid", columns, 10)
            .ChangePageSize(true)
            .WithMultipleFilters()
            .WithGridItemsCount();

        grid = client.Grid;
        task = client.UpdateGrid();
        await task;
    }

    protected async override Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            await GetTasks("odata/user/task");
            signalRClient.OnTaskUpdate += OnTaskUpdate;
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

    private void OnTaskUpdate(TaskModel model)
    {
        var item = grid.Items.SingleOrDefault(i => i.Id == model.Id);
        if (item is not null)
        {
            item.ActualStatus = model.State;
        }
    }

    public void Dispose()
    {
        signalRClient.OnTaskUpdate -= OnTaskUpdate;
    }
}
