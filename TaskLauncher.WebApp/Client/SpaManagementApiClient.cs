using Auth0.ManagementApi;

namespace TaskLauncher.WebApp.Client;

public class SpaManagementApiClient : ManagementApiClient
{
    public SpaManagementApiClient(string domain, IManagementConnection connection) : base("some.token", domain, connection) { }
    public SpaManagementApiClient(string domain) : base("some.token", domain, new HttpClientManagementConnection()) { }
}
