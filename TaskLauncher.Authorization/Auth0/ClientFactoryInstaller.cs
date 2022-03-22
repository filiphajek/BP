using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Microsoft.Extensions.DependencyInjection;

namespace TaskLauncher.Authorization.Auth0;

public static class ClientFactoryInstaller
{
    public static void InstallClientFactories(this IServiceCollection services)
    {
        services.AddSingleton<IClientFactory<AuthenticationApiClient>, AuthenticationApiClientFactory>();
        services.AddSingleton<IClientFactory<ManagementApiClient>, ManagementApiClientFactory>();
    }
}
