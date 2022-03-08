using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using TaskLauncher.Common.Extensions;
using TaskLauncher.Common.Models;

namespace TaskLauncher.WebApp.Client.Pages.User;

public partial class Profile
{
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
        //var tmp = await auth0client.UserBlocks.GetByUserIdAsync(User.UserId); // blocked for
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            Blocked = true
        })).GetModel();
        StateHasChanged();
    }

    async Task UnBanUserAsync()
    {
        User = (await auth0client.Users.UpdateAsync(User.UserId, new()
        {
            Blocked = false
        })).GetModel();
        StateHasChanged();
    }

    async Task CancelAsync()
    {
        //nejakej warning a pak vse smazat
    }
}