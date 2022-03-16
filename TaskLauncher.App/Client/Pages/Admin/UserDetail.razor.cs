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
using static TaskLauncher.App.Client.Pages.Admin.BanDialog;

namespace TaskLauncher.App.Client.Pages.Admin;

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
    private CGrid<TaskResponse> taskGrid;
    private IGridClient<BanResponse> banClient; 

    int tokenBalance = 0;

    protected async Task UpdateBalance()
    {
        var tmp = await client.PutAsJsonAsync("api/token", new UpdateBalanceRequest() { Amount = tokenBalance, UserId = User.UserId });
        if(tmp.IsSuccessStatusCode)
            User.TokenBalance = tokenBalance.ToString();
    }

    protected async override Task OnParametersSetAsync()
    {
        loading = true;
        User = (await auth0client.Users.GetAsync(Id)).GetModel();
        var balance = await client.GetFromJsonAsync<TokenBalanceResponse>($"api/token?userId={Id}");
        User.TokenBalance = balance!.CurrentAmount.ToString();
        tokenBalance = (int)balance!.CurrentAmount;

        //TODO do jednotlivych komponent
        //bans
        Action<IGridColumnCollection<BanResponse>> columns = c =>
        {
            c.Add(o => o.Description);
            c.Add(o => o.Started);
            c.Add(o => o.Ended);
        };
        string url = NavigationManager.BaseUri + $"odata/admin/ban?$filter=userid eq '{Id}'";
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
        string url2 = NavigationManager.BaseUri + $"odata/admin/payment?$filter=userid eq '{Id}'";
        var paymentClient = new GridODataClient<PaymentResponse>(client, url2, new QueryDictionary<StringValues>(), false, "paymentGrid", columns2, 10)
            .ChangePageSize(true)
            .Sortable()
            .WithGridItemsCount();

        paymentGrid = paymentClient.Grid;
        await paymentClient.UpdateGrid();

        //tasks
        Action<IGridColumnCollection<TaskResponse>> columns3 = c =>
        {
            c.Add(o => o.Id).Encoded(false).Sanitized(false).RenderValueAs(o => $"<a href='tasks/{o.Id}'>Detail</a>");
            c.Add(o => o.Name);
            c.Add(o => o.Description);
            c.Add(o => o.ActualStatus);
        };
        string url3 = NavigationManager.BaseUri + $"odata/admin/task?$filter=userid eq '{Id}'";
        var taskClient = new GridODataClient<TaskResponse>(client, url3, new QueryDictionary<StringValues>(), false, "taskGrid", columns3, 10)
            .ChangePageSize(true)
            .Sortable()
            .WithGridItemsCount();

        taskGrid = taskClient.Grid;
        await taskClient.UpdateGrid();

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

        var result = await client.PostAsJsonAsync("api/admin/ban", new BanUserRequest { Reason = res.Reason, UserId = User.UserId });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        await banClient.UpdateGrid();
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        var result = await client.PostAsJsonAsync($"api/admin/ban/cancel?id={User.UserId}", new { });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        await banClient.UpdateGrid();
        StateHasChanged();
    }
}