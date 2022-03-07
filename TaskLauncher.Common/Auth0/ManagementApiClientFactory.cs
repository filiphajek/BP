using Auth0.ManagementApi;

namespace TaskLauncher.Common.Auth0;

public class ManagementApiClientFactory
{
    private readonly ManagementTokenService managementTokenService;
    private readonly ManagementApiClient client;

    public ManagementApiClientFactory(ManagementTokenService managementTokenService)
    {
        this.managementTokenService = managementTokenService;
        client = new("", "");
    }

    public async Task<ManagementApiClient> GetClient()
    {
        var accessToken = await managementTokenService.GetApiToken(new(), "managment_api");
        client.UpdateAccessToken(accessToken);
        return client;
    }
}