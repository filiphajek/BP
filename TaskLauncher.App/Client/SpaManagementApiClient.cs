using Auth0.ManagementApi;

namespace TaskLauncher.App.Client;

/// <summary>
/// Volani auth0 management api z SPA .. dotaz jde pres backend, kde se dosadi hlavicka Authorization
/// </summary>
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