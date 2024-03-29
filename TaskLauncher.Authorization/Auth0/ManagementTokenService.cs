﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using TaskLauncher.Common.Services;

namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// Trida zajistujici obsluhu pristupoveho tokenu k management api nebo protected api
/// Aktualizuje token apod.
/// </summary>
public class ManagementTokenService
{
    private readonly ILogger<ManagementTokenService> logger;
    private readonly Cache<AccessToken> cache;
    private readonly Auth0ApiConfiguration config;

    private class AccessTokenItem
    {
        public string access_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }

    public ManagementTokenService(ILogger<ManagementTokenService> logger, IOptions<Auth0ApiConfiguration> config, Cache<AccessToken> cache)
    {
        this.logger = logger;
        this.cache = cache;
        this.config = config.Value;
    }

    public bool TokenExpired(string api_name)
    {
        var accessToken = cache.Get(api_name);
        if (accessToken is null)
            return false;

        return accessToken.ExpiresIn > DateTime.UtcNow;
    }

    /// <summary>
    /// Zkusi ziskat api token z cache, pokud tam neni nebo token vyprsel, zavola se auth0 a ziska se novy
    /// </summary>
    public async Task<string> GetApiToken(HttpClient client, string api_name, bool audienceIsDomain = true)
    {
        var accessToken = cache.Get(api_name);

        if (accessToken != null)
        {
            if (accessToken.ExpiresIn > DateTime.UtcNow)
            {
                return accessToken.AcessToken;
            }
        }

        logger.LogDebug("New token for management api");

        var newAccessToken = await GetApiTokenClient(client, audienceIsDomain);
        cache.Add(api_name, newAccessToken);

        return newAccessToken.AcessToken;
    }

    /// <summary>
    /// Novy token pro protected api (pokud je audienceIsDomain false) nebo management api
    /// </summary>
    private async Task<AccessToken> GetApiTokenClient(HttpClient client, bool audienceIsDomain)
    {
        string aud = $"https://{config.Domain}/api/v2/";
        if(!audienceIsDomain)
            aud = config.Audience;
        
        try
        {
            var payload = new
            {
                client_id = config.ClientId,
                client_secret = config.ClientSecret,
                audience = aud,
                grant_type = "client_credentials"
            };

            var tokenResponse = await client.PostAsJsonAsync($"https://{config.Domain}/oauth/token/", payload);

            if (tokenResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = await tokenResponse.Content.ReadFromJsonAsync<AccessTokenItem>();
                DateTime expirationTime = DateTime.Now.AddSeconds(result.expires_in);
                return new AccessToken
                {
                    AcessToken = result.access_token,
                    ExpiresIn = expirationTime
                };
            }

            logger.LogError("Status code: {0}, Error: {1}", tokenResponse.StatusCode, tokenResponse.ReasonPhrase);
            throw new ApplicationException($"Status code: {tokenResponse.StatusCode}, Error: {tokenResponse.ReasonPhrase}");
        }
        catch (Exception e)
        {
            logger.LogError("Exception {0}", e);
            throw new ApplicationException($"Exception {e}");
        }
    }
}
