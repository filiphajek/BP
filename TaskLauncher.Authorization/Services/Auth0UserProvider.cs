using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TaskLauncher.Authorization.Auth0;

namespace TaskLauncher.Authorization.Services;

public class Auth0UserProvider : IAuth0UserProvider
{
    private readonly Auth0ApiConfiguration config;
    private readonly IClientFactory<AuthenticationApiClient> authClientFactory;
    private readonly HttpContext httpContext;

    public Auth0UserProvider(IClientFactory<AuthenticationApiClient> authClientFactory, IOptions<Auth0ApiConfiguration> config, IHttpContextAccessor httpContextAccessor)
    {
        this.config = config.Value;
        this.authClientFactory = authClientFactory;
        httpContext = httpContextAccessor.HttpContext!;
    }

    public async Task<AccessTokenResponse> GetRefreshedAccessToken()
    {
        string refreshToken = (await httpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken))!;
        var auth0client = await authClientFactory.GetClient();

        return await auth0client.GetTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            Audience = config.Audience,
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            Scope = "openid profile email",
            SigningAlgorithm = JwtSignatureAlgorithm.RS256
        });
    }

    public async Task<UserInfo> GetActualUser()
    {
        string accessToken = (await httpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken))!;
        var auth0client = await authClientFactory.GetClient();
        return await auth0client.GetUserInfoAsync(accessToken);
    }
}