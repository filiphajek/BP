using Auth0.ManagementApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using Radzen;
using System.Net.Http.Json;
using System.Security.Claims;
using TaskLauncher.Api.Contracts.Requests;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;
using TaskLauncher.WebApp.Client.Pages.Admin;
using static TaskLauncher.WebApp.Client.Pages.Admin.BanDialog;

namespace TaskLauncher.WebApp.Client.Pages.User;

public partial class Profile
{
    [Inject]
    public DialogService DialogService { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    protected HttpClient client { get; set; }   

    [Inject]
    public SpaManagementApiClient auth0client { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Parameter]
    public string Id { get; set; }

    protected bool loading = true;
    protected UserModel User { get; set; } = new();

    protected async override Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            var principal = (await authenticationStateTask).User;
            User = new()
            {
                UserId = principal.Claims.SingleOrDefault(i => i.Type == ClaimTypes.NameIdentifier)!.Value,
                NickName = principal.Claims.SingleOrDefault(i => i.Type == "nickname")!.Value,
                Email = principal.Identity!.Name,
                Picture = principal.Claims.SingleOrDefault(i => i.Type == "picture")!.Value,
                Vip = bool.Parse(principal.Claims.SingleOrDefault(i => i.Type == "https://wutshot-test-api.com/vip")!.Value)
            };
            loading = false;
        }
    }

    protected async override Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(Id))
            return;

        User = (await auth0client.Users.GetAsync(Id)).GetModel();
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
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        var result = await client.PostAsJsonAsync($"api/ban/cancel?id={User.UserId.Remove(0, 6)}", new { });
        User = (await result.Content.ReadFromJsonAsync<UserModel>())!;
        StateHasChanged();
    }

    async Task CancelAsync()
    {
        var tmp = await DialogService.Confirm("Are you sure?", "MyTitle", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
        if (!tmp.HasValue)
            return;
        if (!tmp.Value)
            return;

        await client.DeleteAsync("auth/user/" + User.UserId.Remove(0, 6));
        NavigationManager.NavigateTo("/", true);
    }
}
