using GridBlazor;
using GridShared.Utility;
using Microsoft.Extensions.Primitives;
using GridShared;
using Microsoft.AspNetCore.Components;
using TaskLauncher.App.Client.Services;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Pages.Admin;

public partial class Users
{
    [Inject]
    public IUserProvider userProvider { get; set; }

    protected bool isLoading = false;
    private CGrid<UserModel> grid;
    private Task task;

    protected async override Task OnInitializedAsync()
    {
        isLoading = true;

        Action<IGridColumnCollection<UserModel>> columns = c =>
        {
            c.Add().Encoded(false).Sanitized(false).SetWidth("5%").SetCrudHidden(true).RenderValueAs(o => $"<img width='30' height='30' style='border-radius:50%' src='{o.Picture}' />");
            c.Add(o => o.Email).Encoded(false).Sanitized(false).RenderValueAs(o =>  $"<a href='users/{o.UserId}'>{o.Email}</a>");
            c.Add(o => o.NickName).Titled("Nickname");
            c.Add(o => o.Blocked).RenderValueAs(o => o.Blocked.HasValue ? o.Blocked.Value.ToString() : "False").Titled("Is blocked").Sortable(true).Filterable(true).Css("center-align");//.SetWidth(20);
            c.Add(o => o.Vip).Titled("Is vip").Sortable(true).Filterable(true).Css("center-align");//.SetWidth(20);
        };

        var query = new QueryDictionary<StringValues>();
        var client = new GridClient<UserModel>(async q => await userProvider.GetUsers(columns, q), query, false, "usersGrid", columns)
            .ChangePageSize(true)
            .WithGridItemsCount();

        grid = client.Grid;
        task = client.UpdateGrid();
        await task;

        isLoading = false;
    }
}
