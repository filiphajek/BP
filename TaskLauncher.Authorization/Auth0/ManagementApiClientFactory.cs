using Auth0.ManagementApi;
using Microsoft.Extensions.Options;

namespace TaskLauncher.Authorization.Auth0;

/// <summary>
/// Tovarna na ManagementApiClient
/// </summary>
public class ManagementApiClientFactory : IClientFactory<ManagementApiClient>
{
    private readonly ManagementTokenService managementTokenService;
    private readonly ManagementApiClient client;

    public ManagementApiClientFactory(ManagementTokenService managementTokenService, IOptions<Auth0ApiConfiguration> config)
    {
        this.managementTokenService = managementTokenService;
        client = new("", config.Value.Domain);
    }

    /// <summary>
    /// Vraci ManagementApiClient, ktery bude mit vzdy aktualizovany pristupovy token na managment api
    /// </summary>
    public async Task<ManagementApiClient> GetClient()
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        client.UpdateAccessToken(accessToken);
        return client;
    }
}