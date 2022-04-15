using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
using TaskLauncher.Common.Models;

namespace TaskLauncher.App.Client.Authentication;

/// <summary>
/// Implementace AuthenticationStateProvider komponenty 
/// Inspirovano z https://github.com/berhir/BlazorWebAssemblyCookieAuth od Bernda Hirschmanna a Dominicka Baiera
/// </summary>
public class HostAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    private const string LogInPath = "auth/login";
    private const string LogOutPath = "auth/logout";

    private readonly NavigationManager navigationManager;
    private readonly HttpClient client;

    private DateTimeOffset userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
    private ClaimsPrincipal cachedUser = new(new ClaimsIdentity());

    public HostAuthenticationStateProvider(NavigationManager navigationManager, HttpClient client)
    {
        this.navigationManager = navigationManager;
        this.client = client;
    }

    public void MarkAsBanned()
    {
        var bannedIdentity = new ClaimsIdentity();
        bannedIdentity.AddClaim(new Claim("banned", "true"));
        AuthenticationState state = new(new(bannedIdentity));
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return new AuthenticationState(await GetUser(useCache: true));
    }

    /// <summary>
    /// Presmerovani na prihlaseni
    /// </summary>
    public void SignIn(string? customReturnUrl = null)
    {
        var returnUrl = customReturnUrl != null ? navigationManager.ToAbsoluteUri(customReturnUrl).ToString() : null;
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl ?? navigationManager.Uri);
        var logInUrl = navigationManager.ToAbsoluteUri($"{LogInPath}?returnUrl={encodedReturnUrl}");
        navigationManager.NavigateTo(logInUrl.ToString(), true);
    }

    /// <summary>
    /// Metoda vraci cachnuty ClaimsPrincipal nebo posle dotaz pro stazeni uzivatelskych dat pokud je useCache = true
    /// </summary>
    private async ValueTask<ClaimsPrincipal> GetUser(bool useCache = false)
    {
        var now = DateTimeOffset.Now;
        if (useCache && now < userLastCheck + RefreshInterval)
            return cachedUser;

        try
        {
            cachedUser = await FetchUser();
        }
        catch
        {
            //prazdny claimsprincipal = neprihlasen, neautorizovan
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        userLastCheck = now;
        return cachedUser;
    }

    /// <summary>
    /// Ziskani uzivatelskych dat
    /// </summary>
    public async Task<ClaimsPrincipal> FetchUser()
    {
        var response = await client.GetAsync("auth/user");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var bannedIdentity = new ClaimsIdentity();
            bannedIdentity.AddClaim(new Claim("banned", "true"));
            navigationManager.NavigateTo("/", true);
            return new ClaimsPrincipal(bannedIdentity);
        }
        UserInfo? user = await response.Content.ReadFromJsonAsync<UserInfo>();

        if (user == null || !user.IsAuthenticated)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var identity = new ClaimsIdentity(nameof(HostAuthenticationStateProvider), user.NameClaimType, ClaimTypes.Role);

        if (user.Claims != null)
            identity.AddClaims(user.Claims.Select(i => new Claim(i.Type, i.Value)));

        return new ClaimsPrincipal(identity);
    }
}
