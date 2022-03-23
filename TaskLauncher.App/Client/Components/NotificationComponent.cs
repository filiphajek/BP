using Blazored.Toast;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace TaskLauncher.App.Client.Components;

/// <summary>
/// Komponenta ktera zajisti registraci na hub a prijimani notifikaci
/// Pri aktualizaci stranky se komponenta znovu vyvola
/// </summary>
public class NotificationComponent : ComponentBase
{
    [Inject]
    public IToastService ToastService { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    public SignalRClient SignalRClient { get; set; }

    [CascadingParameter]
    public Task<AuthenticationState> authenticationStateTask { get; set; }

    protected async override Task OnInitializedAsync()
    {
        var state = await authenticationStateTask;

        if (!IsRegistered(state.User) && state.User.Identity!.IsAuthenticated)
        {
            NavigationManager.NavigateTo("registration");
            return;
        }

        if (state.User.Identity!.IsAuthenticated)
        {
            if (SignalRClient.Connection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
            {
                await SignalRClient.TryToConnect();
                //SignalRClient.RegisterOnTaskUpdate(i => ToastService.ShowSuccess($"Task finished: '{i.Id}'"));
                SignalRClient.RegisterOnTaskUpdate(i =>
                {
                    var toastParameters = new ToastParameters();
                    switch (i.State)
                    {
                        case Common.Enums.TaskState.Crashed:
                            ToastService.ShowError($"Task finished: '{i.Id}'");
                            break;
                        case Common.Enums.TaskState.Timeouted:
                            ToastService.ShowError($"Task finished: '{i.Id}'");
                            break;
                        case Common.Enums.TaskState.FinishedSuccess:
                            ToastService.ShowSuccess($"Task finished: '{i.Id}'");
                            break;
                        case Common.Enums.TaskState.FinishedFailure:
                            ToastService.ShowError($"Task finished: '{i.Id}'");
                            break;
                    }
                });
            }
        }
    }

    private bool IsRegistered(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return false;

        var claim = principal.Claims.FirstOrDefault(i => i.Type == "https://wutshot-test-api.com/registered");
        if (claim is null)
            return false;

        return claim.Value == "true";
    }
}
