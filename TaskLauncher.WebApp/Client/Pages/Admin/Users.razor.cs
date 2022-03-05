using GridBlazor;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;
using TaskLauncher.WebApp.Client.Services;
using GridShared;
using Microsoft.AspNetCore.Components;

namespace TaskLauncher.WebApp.Client.Pages.Admin;

public partial class Users
{
    [Inject]
    public IUserProvider userProvider { get; set; }

    protected bool isLoading = false;
    private CGrid<Auth0.ManagementApi.Models.User> grid;
    private Task task;

    protected async override Task OnInitializedAsync()
    {
        isLoading = true;

        Action<IGridColumnCollection<Auth0.ManagementApi.Models.User>> columns = c =>
        {
            c.Add(o => o.Email).Encoded(false).Sanitized(false).RenderValueAs(o =>  $"<a href='profile/{o.UserId}'>{o.Email}</a>");
            c.Add(o => o.NickName);
        };

        var query = new QueryDictionary<StringValues>();
        var client = new GridClient<Auth0.ManagementApi.Models.User>(async q => await userProvider.GetUsers(columns, q), query, false, "usersGrid", columns)
            .Sortable();

        grid = client.Grid;
        task = client.UpdateGrid();
        await task;

        isLoading = false;
    }
}
