using GridBlazor;
using GridBlazor.Pages;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Primitives;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common;
using TaskLauncher.Common.Enums;
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
    private GridComponent<TaskResponse> gridComponent;

    List<SelectItem> taskStates = new()
    {
        new(TaskState.Created.ToString(), TaskState.Created.ToString()),
        new(TaskState.Ready.ToString(), TaskState.Ready.ToString()),
        new(TaskState.Running.ToString(), TaskState.Running.ToString()),
        new(TaskState.Cancelled.ToString(), TaskState.Cancelled.ToString()),
        new(TaskState.Timeouted.ToString(), TaskState.Timeouted.ToString()),
        new(TaskState.FinishedFailure.ToString(), TaskState.FinishedFailure.ToString()),
        new(TaskState.FinishedSuccess.ToString(), TaskState.FinishedSuccess.ToString()),
        new(TaskState.Downloaded.ToString(), TaskState.Downloaded.ToString()),
        new(TaskState.Closed.ToString(), TaskState.Closed.ToString()),
        new(TaskState.Crashed.ToString(), TaskState.Crashed.ToString()),
    };

    async Task GetTasks(string path)
    {
        string url = NavigationManager.BaseUri + path;
        var query = new QueryDictionary<StringValues>();

        //nastaveni gridu
        Action<IGridColumnCollection<TaskResponse>> columns = c =>
        {
            c.Add(o => o.Name).Encoded(false).Sanitized(false).RenderValueAs(o => $"<a href='tasks/{o.Id}'>{o.Name}</a>").Sortable(true).Filterable(true);
            c.Add(o => o.Description).RenderValueAs(o => o.Description.Length > 50 ? o.Description[..50] + " ..." : o.Description).Filterable(true);
            c.Add(o => o.CreationDate).Titled("Creation date").Sortable(true).Filterable(true)
                .SortInitialDirection(GridShared.Sorting.GridSortDirection.Descending);
            c.Add(o => o.IsPriority).Titled("Priorized").Sortable(true).Filterable(true);
            c.Add(o => o.ActualStatus).Sortable(true).Filterable(true).SetListFilter(taskStates, o =>
            {
                o.ShowSelectAllButtons = true;
            });
            c.Add().RenderComponentAs(typeof(Components.ColumnTaskStatus)).SetWidth(150);
        };
        //ziskani tasku
        var gridClient = new GridODataClient<TaskResponse>(Client, url, query, false, "taskGrid", columns, 10)
            .ChangePageSize(true)
            .WithMultipleFilters()
            .WithGridItemsCount();

        grid = gridClient.Grid;
        task = gridClient.UpdateGrid();
        await task;
    }

    protected async override Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            await GetTasks("odata/user/tasks");
            signalRClient.OnTaskFinished += OnTaskUpdate;
        }
        else
        {
            var state = await authenticationStateTask;
            if (!state.User.IsInRole(Constants.Roles.Admin))
            {
                NavigationManager.NavigateTo("tasks", true);
                return;
            }
            await GetTasks($"odata/admin/tasks?userId={Id}");
        }
    }
    
    private void OnTaskUpdate(TaskModel model)
    {
        gridComponent.UpdateGrid().Wait();
    }

    public void Dispose()
    {
        signalRClient.OnTaskFinished -= OnTaskUpdate;
    }
}
