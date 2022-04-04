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

    bool userIsNotVerifiedOrRegistered = false;

    protected async Task UpdateBalance()
    {
        var tmp = await client.PutAsJsonAsync("api/admin/token", new UpdateBalanceRequest() { Amount = tokenBalance, UserId = User.UserId });
        if(tmp.IsSuccessStatusCode)
            User.TokenBalance = tokenBalance.ToString();
    }

    protected async override Task OnParametersSetAsync()
    {
        loading = true;
        User = (await auth0client.Users.GetAsync(Id)).GetModel();

        if (!User.Registered || !User.EmailVerified!.Value)
            userIsNotVerifiedOrRegistered = true;
        else
        {
            var balance = await client.GetFromJsonAsync<TokenBalanceResponse>($"api/admin/token?userid={Id}");
            User.TokenBalance = balance!.CurrentAmount.ToString();
            tokenBalance = (int)balance!.CurrentAmount;
        }
        loading = false;
    }

    async Task UnVipAsync()
    {
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': false }")
        })).GetModel();
        User.TokenBalance = tokenBalance.ToString();
    }

    async Task VipAsync()
    {
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            AppMetadata = JsonConvert.DeserializeObject("{ 'vip': true }")
        })).GetModel();
        User.TokenBalance = tokenBalance.ToString();
    }

    async Task BanUserAsync()
    {
        BanDialogResult res = await DialogService.OpenAsync<BanDialog>($"Give ban to {User.NickName}",
            new Dictionary<string, object>() { { "UserId", User.UserId } },
            new DialogOptions() { Width = "500px", Height = "400px", Resizable = true, Draggable = true });

        var result = await client.PostAsJsonAsync("api/admin/bans", new BanUserRequest { Reason = res.Reason, UserId = User.UserId });
        if (!result.IsSuccessStatusCode)
            return;

        User.Blocked = true;
        await banComponent.BanClient.UpdateGrid();

        banComponent.Refresh();
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        var ban = banComponent.BanClient.Grid.Items.FirstOrDefault(i => i.Ended == null);
        if (ban is null)
            return;

        var result = await client.PostAsJsonAsync($"api/admin/bans/{ban.Id}/cancel", new { });
        if (!result.IsSuccessStatusCode)
            return;

        User.Blocked = false;
        await banComponent.BanClient.UpdateGrid();

        banComponent.Refresh();
        StateHasChanged();
    }
}