using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TaskLauncher.Authorization.Auth0;

namespace TaskLauncher.Authorization.Services;

/// <summary>
/// Implementace IAuth0UserProvider
/// Poskytuje aktualni informace o uzivateli na zaklade jeho pristupoveho tokenu na auth0
/// </summary>
public class Auth0UserProvider : IAuth0UserProvider
{
    private readonly IClientFactory<AuthenticationApiClient> authClientFactory;
    private readonly HttpContext httpContext;

    public Auth0UserProvider(IClientFactory<AuthenticationApiClient> authClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        this.authClientFactory = authClientFactory;
        httpContext = httpContextAccessor.HttpContext!;
    }

    /// <summary>
    /// Poskytuje aktualni informace o uzivateli na zaklade jeho pristupoveho tokenu na auth0
    /// </summary>
    public async Task<UserInfo> GetActualUser()
    {
        string accessToken = (await httpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken))!;
        var auth0client = await authClientFactory.GetClient();
        return await auth0client.GetUserInfoAsync(accessToken);
    }
}