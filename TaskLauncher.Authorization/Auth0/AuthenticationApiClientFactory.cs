﻿using Auth0.AuthenticationApi;
using Microsoft.Extensions.Options;

namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// Tovarna vraci AuthenticationApiClient
/// </summary>
public class AuthenticationApiClientFactory : IClientFactory<AuthenticationApiClient>
{
    private readonly Auth0ApiConfiguration config;
    private readonly IHttpClientFactory httpClientFactory;

    public AuthenticationApiClientFactory(IOptions<Auth0ApiConfiguration> config, IHttpClientFactory httpClientFactory)
    {
        this.config = config.Value;
        this.httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Vraci noveho AuthenticationApiClient
    /// </summary>
    public Task<AuthenticationApiClient> GetClient()
    {
        var client = httpClientFactory.CreateClient();
        return Task.FromResult(new AuthenticationApiClient(new Uri($"https://{config.Domain}"), new HttpClientAuthenticationConnection(client)));
    }
}
