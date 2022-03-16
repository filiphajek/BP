using System.Net;

namespace TaskLauncher.App.Client.Authentication;

/// <summary>
/// Pomocny http handler zabranujici odeslani pozadavku, pokud uzivatel neni prihlaseny, misto toho se presmeruje na login
/// inspirovano z https://github.com/berhir/BlazorWebAssemblyCookieAuth
/// </summary>
public class AuthorizedHandler : DelegatingHandler
{
    private readonly HostAuthenticationStateProvider authenticationStateProvider;

    public AuthorizedHandler(HostAuthenticationStateProvider authenticationStateProvider)
    {
        this.authenticationStateProvider = authenticationStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        HttpResponseMessage responseMessage;

        if (!authState.User.Identity.IsAuthenticated)
            responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        else
            responseMessage = await base.SendAsync(request, cancellationToken);

        //presmerovani na login pokud uzivatel neni prihlasen nebo se ze serveru vrati 401
        if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
            authenticationStateProvider.SignIn();

        return responseMessage;
    }
}
