using Auth0.ManagementApi;
using Microsoft.Extensions.Options;

namespace TaskLauncher.Common.Auth0;

public class ManagementApiClientFactory
{
    private readonly ManagementTokenService managementTokenService;
    private readonly ManagementApiClient client;

    public ManagementApiClientFactory(ManagementTokenService managementTokenService, IOptions<Auth0ApiConfiguration> config)
    {
        this.managementTokenService = managementTokenService;
        client = new("", config.Value.Domain);
    }

    public async Task<ManagementApiClient> GetClient()
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        client.UpdateAccessToken(accessToken);
        return client;
    }
}