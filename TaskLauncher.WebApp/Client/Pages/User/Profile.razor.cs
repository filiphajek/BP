using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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
}
