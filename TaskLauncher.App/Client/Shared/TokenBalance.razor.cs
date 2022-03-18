using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TaskLauncher.App.Client.Store;
using TaskLauncher.Authorization;
using TaskLauncher.Common.Extensions;

namespace TaskLauncher.App.Client.Shared;

public partial class TokenBalance : IDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> authenticationStateTask { get; set; }

    [Inject]
    public TokenStore TokenStore { get; set; }

    string balance = "";

    protected async override Task OnInitializedAsync()
    {
        var state = await authenticationStateTask;
        state.User.TryGetClaimValue(TaskLauncherClaimTypes.TokenBalance, out balance);
        TokenStore.OnBalanceChange += Handler;
    }

    async Task Handler()
    {
        balance = await TokenStore.GetBalanceAsync();
        StateHasChanged();
    }

    public void Dispose()
    {
        TokenStore.OnBalanceChange -= Handler;
    }
}
