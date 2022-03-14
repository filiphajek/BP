using Microsoft.AspNetCore.Components;
using System.Net;

namespace TaskLauncher.WebApp.Client.Authentication;

public class BanHandler : DelegatingHandler
{
    private readonly HostAuthenticationStateProvider authenticationStateProvider;
    private readonly NavigationManager navigationManager;

    public BanHandler(HostAuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager)
    {
        this.authenticationStateProvider = authenticationStateProvider;
        this.navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var responseMessage = await base.SendAsync(request, cancellationToken);

        //TODO testnout to bez tohoto .. uz dostavam forbidem tak jako tak ne??????

        //pokud je uzivatel zabanovany, odhlas ho a presmeruj ho na tuto stranku
        if (responseMessage.StatusCode == HttpStatusCode.Forbidden)
        {
            authenticationStateProvider.MarkAsBanned();
            navigationManager.NavigateTo("/banned", true);
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        }
        /*var authState = await authenticationStateProvider.FetchUser();
        if(!authState.Identity!.IsAuthenticated)
        {
            authenticationStateProvider.MarkAsBanned();
            navigationManager.NavigateTo("/banned", true);
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        }*/

        return responseMessage;
    }
}