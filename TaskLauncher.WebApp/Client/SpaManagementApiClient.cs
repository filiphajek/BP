using Auth0.ManagementApi;

namespace TaskLauncher.WebApp.Client;

public class SpaManagementApiClient : ManagementApiClient
{
    public SpaManagementApiClient(Auth0ApiClientConfiguration options) 
        : base("some.token", $"{options.Domain}/{options.Endpoint}", new HttpClientManagementConnection()) { }
}

public class Auth0ApiClientConfiguration
{
    public string Domain { get; set; }
    public string Endpoint { get; set; }
}