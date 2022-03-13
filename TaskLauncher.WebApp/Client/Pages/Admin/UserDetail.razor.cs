using GridBlazor;
using GridShared;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Radzen;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using static TaskLauncher.WebApp.Client.Pages.Admin.BanDialog;

namespace TaskLauncher.WebApp.Client.Pages.Admin;

public partial class UserDetail
{
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    public DialogService DialogService { get; set; }

    [Inject]
    protected HttpClient client { get; set; }

    [Inject]
    public SpaManagementApiClient auth0client { get; set; }

    [Parameter]
    public string Id { get; set; }

    protected bool loading = false;
    protected UserModel User { get; set; } = new();

    protected bool isLoading = false;
    private CGrid<BanResponse> banGrid;
    private CGrid<PaymentResponse> paymentGrid;
    private IGridClient<BanResponse> banClient; 


    protected async override Task OnParametersSetAsync()
    {
        loading = true;
        User = (await auth0client.Users.GetAsync(Id)).GetModel();

        //bans
        Action<IGridColumnCollection<BanResponse>> columns = c =>
        {
            c.Add(o => o.Email);
            c.Add(o => o.Description);
            c.Add(o => o.Started);
            c.Add(o => o.Ended);
        };
        string url = NavigationManager.BaseUri + $"odata/admin/ban";//?userId={Id}
        var query = new QueryDictionary<StringValues>();
        banClient = new GridODataClient<BanResponse>(client, url, query, false, "banGrid", columns, 10)
            .ChangePageSize(true)
            .Sortable()
            .WithGridItemsCount();

        banGrid = banClient.Grid;
        await banClient.UpdateGrid();

        //payments
        Action<IGridColumnCollection<PaymentResponse>> columns2 = c =>
        {
            c.Add(o => o.Price);
            c.Add(o => o.Time);
            c.Add(o => o.Id);
        };
        string url2 = NavigationManager.BaseUri + $"odata/admin/payment";//?userId={Id}
        var paymentClient = new GridODataClient<PaymentResponse>(client, url2, new QueryDictionary<StringValues>(), false, "paymentGrid", columns2, 10)
            .ChangePageSize(true)
            .Sortable()
            .WithGridItemsCount();

        paymentGrid = paymentClient.Grid;
        await paymentClient.UpdateGrid();

        loading = false;
    }

    async Task UnVipAsync()
    {
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false }")
        })).GetModel();
    }

    async Task VipAsync()
    {
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true }")
        })).GetModel();
    }

    async Task BanUserAsync()
    {
        BanDialogResult res = await DialogService.OpenAsync<BanDialog>($"Give ban to {User.NickName}",
            new Dictionary<string, object>() { { "UserId", User.UserId } },
            new DialogOptions() { Width = "500px", Height = "400px", Resizable = true, Draggable = true });

        var result = await client.PostAsJsonAsync("api/ban", new BanUserRequest { Reason = res.Reason, UserId = User.UserId });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        await banClient.UpdateGrid();
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        var result = await client.PostAsJsonAsync($"api/ban/cancel?id={User.UserId.Remove(0, 6)}", new { });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        await banClient.UpdateGrid();
        StateHasChanged();
    }
}