using GridBlazor;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using System.Net.Http.Json;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Api.Contracts.Responses;
using TaskLauncher.App.Client.Components;
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
    protected ApiClient client { get; set; }

    [Inject]
    public SpaManagementApiClient auth0client { get; set; }

    [Parameter]
    public string Id { get; set; }

    protected bool loading = false;
    protected UserModel User { get; set; } = new();

    protected bool isLoading = false;

    private BanComponent banComponent;

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
        var balance = await client.GetFromJsonAsync<TokenBalanceResponse>($"api/token/{Id}");
        User.TokenBalance = balance!.CurrentAmount.ToString();
        tokenBalance = (int)balance!.CurrentAmount;
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
        await banComponent.BanClient.UpdateGrid();
        banComponent.Refresh();
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        var result = await client.PostAsJsonAsync($"api/admin/ban/cancel?id={User.UserId}", new { });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        await banComponent.BanClient.UpdateGrid();
        banComponent.Refresh();
        StateHasChanged();
    }
}